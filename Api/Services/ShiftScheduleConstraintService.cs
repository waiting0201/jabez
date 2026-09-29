using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 某一格被鎖定為「只能是上班日」的原因。
/// </summary>
/// <param name="Reason"><see cref="ShiftScheduleConstraintService.LockActivity"/> / <see cref="ShiftScheduleConstraintService.LockLeave"/></param>
/// <param name="Label">顯示文字（活動名稱或假別，例「年假」「事假（簽核中）」）</param>
public sealed record ShiftDayLock(string Reason, string Label);

/// <summary>
/// 排班「能不能存」的 I/O 部分（靜態，比照 LeaveRevocationService 慣例：不呼叫 SaveChanges）。
///
/// <see cref="ShiftScheduleValidator"/> 是純函式、只看日別；本服務補上兩條需要查 DB 的限制，
/// 並把兩者合併成一份結果。**個人排班（GET / PUT）與〈改班申請〉（試算 / 存檔 / 送簽 / 終局核准）
/// 全部走 <see cref="EvaluateAsync"/>**，申請人、審核者、月曆看到的檢核結果因此一致。
///
/// 兩條鎖定（2026-09-28 客戶要求）：
/// <list type="bullet">
///   <item><b>活動日預定人力</b>：主管已指派的活動日，本人不得排為例假日／休假日。</item>
///   <item><b>已請假日</b>：簽核中或已核准的假單所涵蓋的日子（扣除已核准銷假），不得排為例假日／休假日 ——
///         否則同一天既是休假又請假，假別時數與排休配額會重複計算。逐日展開走 <see cref="LeaveDayExpander"/>
///         （與請假時數同一真相），故假單中被行事曆扣掉的日子不會被鎖。</item>
/// </list>
/// 國定假日本來就唯讀，不在此列。
/// </summary>
public static class ShiftScheduleConstraintService
{
    public const string LockActivity = "activity";
    public const string LockLeave    = "leave";

    /// <summary>會鎖定排班的假單狀態（草稿、退回、拒絕、已銷假不鎖）。</summary>
    private static readonly string[] LockingLeaveStatuses = ["pending", "approved"];

    /// <summary>該員 [monthStart, monthEnd] 內被鎖為上班日的日子。</summary>
    public static async Task<Dictionary<DateTime, ShiftDayLock>> LoadLockedDatesAsync(
        AppDbContext db, IEmployeeWorkdaysFactory workdaysFactory,
        Guid userId, DateTime monthStart, DateTime monthEnd)
    {
        var locks = new Dictionary<DateTime, ShiftDayLock>();

        // ① 活動日預定人力（同一天多個活動時取本人被指派的那一筆）
        var activities = await db.ActivityDays.AsNoTracking()
            .Where(a => a.Date >= monthStart && a.Date <= monthEnd
                     && a.Assignees.Any(x => x.UserId == userId))
            .OrderBy(a => a.Id)
            .Select(a => new { a.Date, a.Title })
            .ToListAsync();
        foreach (var a in activities)
            locks.TryAdd(a.Date.Date, new ShiftDayLock(LockActivity, a.Title));

        // ② 請假（簽核中 + 已核准，扣掉已核准銷假日）
        var leaves = await db.LeaveRequests.AsNoTracking()
            .Where(l => l.EmployeeId == userId
                     && LockingLeaveStatuses.Contains(l.ApprovalStatus)
                     && l.StartDate < monthEnd.AddDays(1) && l.EndDate >= monthStart)
            .OrderBy(l => l.StartDate)
            .ToListAsync();
        if (leaves.Count == 0) return locks;

        var leaveIds = leaves.Select(l => l.Id).ToList();
        var revoked = (await db.LeaveRevocationDates.AsNoTracking()
                .Where(d => leaveIds.Contains(d.LeaveRevocation!.LeaveRequestId)
                         && d.LeaveRevocation.ApprovalStatus == "approved")
                .Select(d => new { d.LeaveRevocation!.LeaveRequestId, d.Date })
                .ToListAsync())
            .Select(x => (x.LeaveRequestId, x.Date.Date))
            .ToHashSet();

        // 切換日起以本人**目前已存的**班表展開（只鎖真正算請假的上班日）；
        // 若改用公司行事曆，排休在平日的人會被鎖住一個本來就不上班的日子、整月存不了
        var workdays = await workdaysFactory.ForAsync(userId);

        var labels = new Dictionary<DateTime, List<string>>();
        foreach (var leave in leaves)
        {
            var label = LeaveTypeNames.GetZh(leave.LeaveType)
                      + (leave.ApprovalStatus == "pending" ? "（簽核中）" : "");
            var days = await LeaveDayExpander.ExpandAsync(workdays, leave);
            foreach (var day in days)
            {
                var d = day.Date.Date;
                if (d < monthStart || d > monthEnd) continue;
                if (revoked.Contains((leave.Id, d))) continue;
                if (!labels.TryGetValue(d, out var list)) labels[d] = list = [];
                if (!list.Contains(label)) list.Add(label);
            }
        }

        // 活動日與請假同一天時以請假為準（顯示假別較有資訊量，兩者效果相同）
        foreach (var (d, list) in labels)
            locks[d] = new ShiftDayLock(LockLeave, string.Join("、", list));

        return locks;
    }

    /// <summary>
    /// 關卡 A／B 的跨月上下文：前月月底與次月月初的**已定案**班表。
    /// 次月尚未排定就不放進去 —— 未知的日子不可當成上班日，否則會誤擋（見 ShiftScheduleValidator）。
    /// </summary>
    public static async Task<Dictionary<DateTime, string>> LoadContextDaysAsync(
        AppDbContext db, ICalendarDayReadService calendarReader,
        Guid userId, DateTime monthStart, DateTime monthEnd)
    {
        var from = monthStart.AddDays(-(ShiftScheduleValidator.RollingWindowDays - 1));
        var to   = monthEnd.AddDays(ShiftScheduleValidator.RollingWindowDays - 1);

        var saved = await db.ShiftScheduleDays.AsNoTracking()
            .Where(d => d.UserId == userId
                     && ((d.Date >= from && d.Date < monthStart) || (d.Date > monthEnd && d.Date <= to)))
            .ToDictionaryAsync(d => d.Date.Date, d => WorkDayTypes.Normalize(d.DayType));

        // 前後月的國定假日同樣要納入（它們會中斷連續上班、但不算例假）
        var holidays = await ShiftScheduleMap.LoadPublicHolidaysAsync(calendarReader, from, to);
        foreach (var kv in holidays)
        {
            if (kv.Key >= monthStart && kv.Key <= monthEnd) continue;
            saved[kv.Key] = WorkDayTypes.PublicHoliday;
        }
        return saved;
    }

    /// <summary>被鎖的日子卻排成例假／休假 → 擋存訊息。</summary>
    public static List<string> LockBlocks(
        IReadOnlyDictionary<DateTime, string> monthDays, IReadOnlyDictionary<DateTime, ShiftDayLock> locks)
    {
        var blocks = new List<string>();
        foreach (var (d, l) in locks.OrderBy(kv => kv.Key))
        {
            if (!monthDays.TryGetValue(d, out var t)) continue;
            if (t is not (WorkDayTypes.StatutoryOff or WorkDayTypes.RestDay)) continue;

            blocks.Add(l.Reason == LockActivity
                ? $"{d:M/d} 為您的活動日（{l.Label}），不可排定為{WorkDayTypeNames.GetZh(t)}。"
                : $"{d:M/d} 已請{l.Label}，不可排定為{WorkDayTypeNames.GetZh(t)}。");
        }
        return blocks;
    }

    /// <summary>
    /// 整月檢核：<see cref="ShiftScheduleValidator"/> 三條硬性規則 ＋ 活動日／請假鎖定。
    /// </summary>
    /// <param name="monthDays">當月每一天的日別（<see cref="ShiftScheduleMap.BuildMonthDayTypes"/> 的結果）。</param>
    /// <param name="locks">可由呼叫端先載好傳入（GET 也要拿它組 DTO），null 則自行載入。</param>
    public static async Task<ShiftScheduleValidationResult> EvaluateAsync(
        AppDbContext db, ICalendarDayReadService calendarReader, IEmployeeWorkdaysFactory workdaysFactory,
        Guid userId, int year, int month,
        IReadOnlyDictionary<DateTime, string> monthDays,
        IReadOnlyDictionary<DateTime, ShiftDayLock>? locks = null)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);

        locks ??= await LoadLockedDatesAsync(db, workdaysFactory, userId, monthStart, monthEnd);
        var context = await LoadContextDaysAsync(db, calendarReader, userId, monthStart, monthEnd);
        var result  = ShiftScheduleValidator.Validate(year, month, monthDays, context);

        var lockBlocks = LockBlocks(monthDays, locks);
        if (lockBlocks.Count == 0) return result;

        return result with
        {
            CanSave = false,
            Blocks  = [.. lockBlocks, .. result.Blocks],
        };
    }

    public static ShiftScheduleValidationDto ToDto(ShiftScheduleValidationResult r) => new(
        r.CanSave,
        [.. r.Blocks],
        [.. r.Warnings],
        r.StatutoryOffCount,
        r.RestDayCount,
        r.RequiredStatutoryOff,
        r.RequiredRestDay,
        r.RequiredOffDays);
}
