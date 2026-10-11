using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 某一格被鎖定為「只能是上班日」的原因。
/// </summary>
/// <param name="Reason"><see cref="ShiftScheduleConstraintService.LockActivity"/> / <see cref="ShiftScheduleConstraintService.LockLeave"/></param>
/// <param name="Label">顯示文字（活動名稱或假別，例「年假」「事假（簽核中）」）</param>
public sealed record ShiftDayLock(string Reason, string Label);

/// <summary>前後月的一天。<paramref name="DayType"/> 為 null ＝ 該月尚未定案、日別未知。</summary>
public sealed record ShiftAdjacentDay(DateTime Date, string? DayType, string? HolidayName);

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
    /// 前月月底與次月月初（各 13 天，恰為滾動 14 天視窗會跨出去的範圍）逐日的日別。
    /// **月曆顯示與關卡 A／B 的跨月檢核共用這一份**，同仁在月曆上看到的前後月就是檢核看到的前後月。
    ///
    /// 判定順序：國定假日 → 已落地的例假／休假 → 該月<b>已定案</b>（committed / auto）則為上班日 → 其餘為 null（未排定）。
    /// ⚠ 第三步不可省：上班日不落地（查無紀錄即上班日），只讀 <c>ShiftScheduleDays</c> 的話
    /// 前後月的上班日全變成「未知」，連續上班天數在月界歸零、跨月的 14 天視窗也全被略過，跨月檢核形同虛設。
    /// 未定案的月份（次月還沒排、切換日前的舊制月份）維持 null —— 不可當成上班日，否則會誤擋。
    /// </summary>
    public static async Task<List<ShiftAdjacentDay>> LoadAdjacentDaysAsync(
        AppDbContext db, ICalendarDayReadService calendarReader,
        Guid userId, DateTime monthStart, DateTime monthEnd)
    {
        var from = monthStart.AddDays(-(ShiftScheduleValidator.RollingWindowDays - 1));
        var to   = monthEnd.AddDays(ShiftScheduleValidator.RollingWindowDays - 1);

        var saved = await db.ShiftScheduleDays.AsNoTracking()
            .Where(d => d.UserId == userId
                     && ((d.Date >= from && d.Date < monthStart) || (d.Date > monthEnd && d.Date <= to)))
            .ToDictionaryAsync(d => d.Date.Date, d => WorkDayTypes.Normalize(d.DayType));

        // from / to 必然分別落在前月與次月（13 天不會跨過整個月）
        var settled = (await db.ShiftScheduleMonths.AsNoTracking()
                .Where(m => m.UserId == userId
                         && (m.Status == ShiftScheduleMonthStatus.Committed || m.Status == ShiftScheduleMonthStatus.Auto)
                         && ((m.Year == from.Year && m.Month == from.Month) || (m.Year == to.Year && m.Month == to.Month)))
                .Select(m => new { m.Year, m.Month })
                .ToListAsync())
            .Select(m => (m.Year, m.Month))
            .ToHashSet();

        // 前後月的國定假日同樣要納入（它們會中斷連續上班、但不算例假）
        var holidays = await ShiftScheduleMap.LoadPublicHolidaysAsync(calendarReader, from, to);

        var result = new List<ShiftAdjacentDay>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            if (d >= monthStart && d <= monthEnd) continue;

            if (holidays.TryGetValue(d, out var name))
                result.Add(new ShiftAdjacentDay(d, WorkDayTypes.PublicHoliday, name));
            else if (saved.TryGetValue(d, out var t))
                result.Add(new ShiftAdjacentDay(d, t, null));
            else
                result.Add(new ShiftAdjacentDay(d, settled.Contains((d.Year, d.Month)) ? WorkDayTypes.Work : null, null));
        }
        return result;
    }

    /// <summary>
    /// 關卡 A／B 的跨月上下文：<see cref="LoadAdjacentDaysAsync"/> 中日別已知的日子。
    /// 未排定（null）的日子不放進去 —— 未知的日子不可當成上班日，否則會誤擋（見 ShiftScheduleValidator）。
    /// </summary>
    public static async Task<Dictionary<DateTime, string>> LoadContextDaysAsync(
        AppDbContext db, ICalendarDayReadService calendarReader,
        Guid userId, DateTime monthStart, DateTime monthEnd)
        => ToContext(await LoadAdjacentDaysAsync(db, calendarReader, userId, monthStart, monthEnd));

    public static Dictionary<DateTime, string> ToContext(IEnumerable<ShiftAdjacentDay> adjacent)
        => adjacent.Where(a => a.DayType is not null).ToDictionary(a => a.Date, a => a.DayType!);

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
    /// <param name="context">跨月上下文，同上（GET 也要拿前後月組 DTO），null 則自行載入。</param>
    public static async Task<ShiftScheduleValidationResult> EvaluateAsync(
        AppDbContext db, ICalendarDayReadService calendarReader, IEmployeeWorkdaysFactory workdaysFactory,
        Guid userId, int year, int month,
        IReadOnlyDictionary<DateTime, string> monthDays,
        IReadOnlyDictionary<DateTime, ShiftDayLock>? locks = null,
        IReadOnlyDictionary<DateTime, string>? context = null)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);

        locks ??= await LoadLockedDatesAsync(db, workdaysFactory, userId, monthStart, monthEnd);
        context ??= await LoadContextDaysAsync(db, calendarReader, userId, monthStart, monthEnd);
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
