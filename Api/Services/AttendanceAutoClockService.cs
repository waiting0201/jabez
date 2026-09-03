using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 登入時的自動補卡共用邏輯（靜態，比照 <see cref="LeaveRevocationService"/> 慣例：
/// 不呼叫 SaveChanges，交易邊界交給呼叫端）。
///
/// <para><b>只填「既有紀錄的空欄」，絕不建立新列。</b>
/// 完全沒有任何打卡痕跡的日子（含只請了半天假卻整天沒打卡者）不予補卡 ——
/// 系統沒有任何證據可證明當事人有出勤，代打就等於憑空產生一整天的出勤紀錄。
/// 那類日子交由出缺勤報表的缺勤 / 未打卡虛擬列呈現，由管理者人工判斷後補登。</para>
///
/// 三種缺口：
///   ① 有下班卡或加班卡、沒有上班卡 → 補上班卡（僅工作日，時間為當日應出勤起）
///   ② 有上班卡、沒有下班卡         → 補下班卡（上班 + 9 小時，被請假蓋掉時提前）
///   ③ 有加班開始、沒有加班結束     → 補加班結束卡（加班開始 + 申請單預估時數）
///
/// 補卡時間一律避開當日已核准請假時段（走 <see cref="ExpectedWorkWindow"/>），
/// 否則補出來的卡會落在請假區間內，與 AttendanceHandler.EnsureNotOnLeaveAsync 的規則自相矛盾。
/// </summary>
public static class AttendanceAutoClockService
{
    /// <summary>自動補下班卡的時數＝標準工時 + 午休（一律 +9，不分上下午打卡）</summary>
    private const int AutoClockOutHours =
        WorkdayHours.FullDayHours + (WorkdayHours.LunchEndHour - WorkdayHours.LunchStartHour);

    /// <summary>
    /// 套用自動補卡。呼叫端負責 SaveChangesAsync。
    /// </summary>
    /// <param name="canClockIn">
    /// 呼叫者是否持有 attendances:write。沒有打卡權限的角色（顧問 / 外部人員）本來就不打卡，
    /// 不補上班卡；與出缺勤報表缺勤列的員工母體同一條規則。
    /// </param>
    public static async Task<AutoClockResult> ApplyAsync(
        AppDbContext db, ICalendarDayReadService calendarReader, User user, bool canClockIn)
    {
        var today = Clock.Now.Date;

        // 三種缺口一次撈回（皆限 RecordDate < today：今天還有機會自己打）
        var pending = await db.AttendanceRecords
            .Include(a => a.OvertimeRequest)
            .Where(a => a.UserId == user.Id
                && a.RecordDate < today
                && ((a.ClockInTime == null && (a.ClockOutTime != null || a.OvertimeStartTime != null))
                 || (a.ClockInTime != null && a.ClockOutTime == null)
                 || (a.OvertimeStartTime != null && a.OvertimeEndTime == null)))
            .ToListAsync();

        if (pending.Count == 0) return AutoClockResult.Empty;

        // 需要「應出勤時段」的日子＝會補上班卡或下班卡者。只補加班結束卡時不必查行事曆與假單。
        var needWindow = pending
            .Where(a => NeedsClockIn(a) || (a.ClockInTime != null && a.ClockOutTime == null))
            .Select(a => a.RecordDate.Date)
            .ToHashSet();

        var cal = new CachedCalendarDayReadService(calendarReader);
        var leavesByDay = needWindow.Count == 0
            ? []
            : await ExpandLeavesAsync(db, cal, user, needWindow);

        // 補上班卡另需工作日判定（休假日只含加班時間的紀錄不該被補上班卡）
        HashSet<DateTime>? workingDates = null;
        var clockInDates = pending.Where(NeedsClockIn).Select(a => a.RecordDate.Date).ToList();
        if (canClockIn && clockInDates.Count > 0)
        {
            var (_, _, working) = await WorkCalendarHelper.ComputeWorkingDatesAsync(
                cal, user.IsShiftWorker, clockInDates.Min(), clockInDates.Max());
            workingDates = [.. working];
        }

        var filledClockIn  = new List<DateTime>();
        var filledClockOut = new List<DateTime>();
        var filledOvertime = new List<DateTime>();

        foreach (var record in pending)
        {
            var date   = record.RecordDate.Date;
            var window = ExpectedWorkWindow.Compute(
                date, leavesByDay.TryGetValue(date, out var dayLeaves) ? dayLeaves : []);

            // ① 補上班卡：僅工作日、當日並非全日請假
            if (NeedsClockIn(record)
                && canClockIn
                && workingDates?.Contains(date) == true
                && window.Start is { } expectedStart)
            {
                record.ClockInTime   = expectedStart;
                record.IsClockInAuto = true;
                filledClockIn.Add(date);
            }

            // ② 補下班卡＝上班打卡時間 + 9 小時。
            //    刻意不用 SystemSetting.WorkEndTime —— 該設定只服務打卡提醒的時點判斷，
            //    且固定補到 18:00 會讓早到 / 晚到者的工時失真。
            //    ⚠️ 只有「當日有假把下班時段蓋掉」時才提前（EndAdjustedByLeave 為閘門）：
            //    無請假時 window.End 恆為 17:00，無條件取 min 會把 09:00 上班者從 18:00 壓成 17:00。
            if (record.ClockInTime is { } clockIn && record.ClockOutTime is null)
            {
                var target = clockIn.AddHours(AutoClockOutHours);
                if (window.EndAdjustedByLeave && window.End is { } expectedEnd
                    && expectedEnd < target && expectedEnd > clockIn)
                    target = expectedEnd;

                record.ClockOutTime   = target;
                record.IsClockOutAuto = true;   // 供出缺勤清單標示「系統補卡」
                filledClockOut.Add(date);
            }

            // ③ 補加班結束卡
            if (record.OvertimeStartTime is { } overtimeStart && record.OvertimeEndTime is null)
            {
                var hours = (double)(record.OvertimeRequest?.EstimatedHours ?? 0);
                record.OvertimeEndTime = overtimeStart.AddHours(hours);
                filledOvertime.Add(date);
            }
        }

        return new AutoClockResult(
            filledClockIn.Count  == 0 ? null : new AutoClockInInfo(filledClockIn.Count, ToDateStrings(filledClockIn)),
            filledClockOut.Count == 0 ? null : new AutoClockOutInfo(filledClockOut.Count, ToDateStrings(filledClockOut)),
            filledOvertime.Count == 0 ? null : new AutoOvertimeEndInfo(filledOvertime.Count, ToDateStrings(filledOvertime)));
    }

    /// <summary>該列有下班卡或加班卡、卻沒有上班卡 —— 人確實來過，只是漏打上班</summary>
    private static bool NeedsClockIn(AttendanceRecord a) =>
        a.ClockInTime is null && (a.ClockOutTime is not null || a.OvertimeStartTime is not null);

    /// <summary>
    /// 把該員工與目標日期有交集的已核准請假逐日展開（扣掉已核准銷假日），依日期分組。
    /// 排班制旗標一律以「假單所有人」解析，此處呼叫者即本人。
    /// </summary>
    private static async Task<Dictionary<DateTime, List<LeaveDay>>> ExpandLeavesAsync(
        AppDbContext db, ICalendarDayReadService cal, User user, HashSet<DateTime> targetDates)
    {
        var minDate = targetDates.Min();
        var maxDate = targetDates.Max();

        var leaves = await db.LeaveRequests.AsNoTracking()
            .Where(l => l.EmployeeId == user.Id
                && l.ApprovalStatus == "approved"
                && l.StartDate.Date <= maxDate
                && l.EndDate.Date   >= minDate)
            .Select(l => new { l.Id, l.LeaveType, l.StartDate, l.EndDate })
            .ToListAsync();

        var result = new Dictionary<DateTime, List<LeaveDay>>();
        if (leaves.Count == 0) return result;

        var leaveIds = leaves.Select(l => l.Id).ToList();
        var revoked = (await db.LeaveRevocationDates.AsNoTracking()
                .Where(d => d.LeaveRevocation!.ApprovalStatus == "approved"
                         && leaveIds.Contains(d.LeaveRevocation.LeaveRequestId))
                .Select(d => new { d.LeaveRevocation!.LeaveRequestId, d.Date })
                .ToListAsync())
            .ToLookup(x => x.LeaveRequestId, x => x.Date.Date);

        foreach (var leave in leaves)
        {
            var revokedSet = revoked[leave.Id].ToHashSet();
            var days = await LeaveDayExpander.ExpandAsync(
                cal, user.IsShiftWorker, leave.LeaveType, leave.StartDate, leave.EndDate);

            foreach (var d in days)
            {
                var date = d.Date.Date;
                if (!targetDates.Contains(date))  continue;
                if (revokedSet.Contains(date))    continue;
                if (d.Hours <= 0)                 continue;

                if (!result.TryGetValue(date, out var list))
                    result[date] = list = [];
                list.Add(d);
            }
        }
        return result;
    }

    private static string[] ToDateStrings(List<DateTime> dates) =>
        [.. dates.Select(d => d.ToString("yyyy-MM-dd")).OrderBy(d => d)];
}
