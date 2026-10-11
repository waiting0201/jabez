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
/// 兩種缺口（2026-10 防灌工時後；原有的第三種「補加班結束卡」已**取消**）：
///   ① 有下班卡或加班卡、沒有上班卡 → 補上班卡（僅工作日，時間為當日應出勤起）
///   ② 有上班卡、沒有下班卡         → 補下班卡（上班 + 9 小時，被請假蓋掉時提前）
///      —— 僅在「該日是該員工的工作日」且「當日並非全日請假」才補。
///         2026-10-07 拿掉「上班打卡時間須落在應出勤起點 −2h～+3h」的合理性檢查：
///         遲到很久才到班（例：14:58 上班）的人也確實來過，不補的話整天沒有下班時間。
///         改由出缺勤報表把所有沒打下班卡的列（含系統補的）標紅字「未打下班卡」，
///         HR 於編輯填寫備註（原因）後轉綠字留存 —— 把關從「不補」改為「補了但必須有人看過」。
///
/// <b>不補加班結束卡</b>：以前會把結束卡補成「加班開始 + 申請單預估時數」，結果申請 14 小時就憑空補出 14 小時
/// （正式資料 12 張）。現在加班給付依實際打卡結算（<see cref="OvertimeSettlement"/>），沒打結束卡就留空、結算為 0，
/// 由管理者於出缺勤報表補正（補正時會自動重算結算時數）。
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
    /// <param name="workdays">本人的工作日判定（切換日起看個人排班，見 <see cref="EmployeeWorkdays"/>）。</param>
    public static async Task<AutoClockResult> ApplyAsync(
        AppDbContext db, EmployeeWorkdays workdays, User user, bool canClockIn)
    {
        var today = Clock.Now.Date;

        // 兩種缺口一次撈回（皆限 RecordDate < today：今天還有機會自己打）
        var pending = await db.AttendanceRecords
            .Where(a => a.UserId == user.Id
                && a.RecordDate < today
                && ((a.ClockInTime == null && (a.ClockOutTime != null || a.OvertimeStartTime != null))
                 || (a.ClockInTime != null && a.ClockOutTime == null)))
            .ToListAsync();

        if (pending.Count == 0) return AutoClockResult.Empty;

        // 需要「應出勤時段」的日子＝會補上班卡或下班卡者。只補加班結束卡時不必查行事曆與假單。
        var needWindow = pending
            .Where(a => NeedsClockIn(a) || (a.ClockInTime != null && a.ClockOutTime == null))
            .Select(a => a.RecordDate.Date)
            .ToHashSet();

        var leavesByDay = needWindow.Count == 0
            ? []
            : await ExpandLeavesAsync(db, workdays, user, needWindow);

        // 補上班卡、補下班卡都需工作日判定：休假日只含加班時間的紀錄不該被補上班卡，
        // 非工作日（例假 / 休假 / 國定假日）上有上班卡卻沒下班卡者，也不補下班卡
        // （防「假日偷偷打上班卡、靠補卡湊出 9 小時」）。
        HashSet<DateTime>? workingDates = null;
        var workdayCheckDates = pending
            .Where(a => (NeedsClockIn(a) && canClockIn) || (a.ClockInTime != null && a.ClockOutTime == null))
            .Select(a => a.RecordDate.Date)
            .ToList();
        if (workdayCheckDates.Count > 0)
        {
            // Attendance 語意：彈性休假日仍是休假日，不該被補上班卡
            var (_, _, working) = await workdays.ComputeAsync(
                workdayCheckDates.Min(), workdayCheckDates.Max(), CalendarScope.Attendance);
            workingDates = [.. working];
        }

        var filledClockIn  = new List<DateTime>();
        var filledClockOut = new List<DateTime>();

        foreach (var record in pending)
        {
            var date   = record.RecordDate.Date;
            // 該日適用的工作時段依「該日日期」與切換日選用（舊制 08:00–17:00 / 新制 09:00–18:00）
            var window = ExpectedWorkWindow.Compute(
                date, leavesByDay.TryGetValue(date, out var dayLeaves) ? dayLeaves : [],
                workdays.ScheduleFor(date));

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
            if (record.ClockInTime is { } clockIn && record.ClockOutTime is null
                && window.Start is not null      // 全日請假卻有上班卡屬異常，不補，留紅字由 HR 判斷
                && workingDates?.Contains(date) == true)
            {
                var target = clockIn.AddHours(AutoClockOutHours);
                if (window.EndAdjustedByLeave && window.End is { } expectedEnd
                    && expectedEnd < target && expectedEnd > clockIn)
                    target = expectedEnd;

                record.ClockOutTime   = target;
                record.IsClockOutAuto = true;   // 供出缺勤清單標示「未打下班卡」（紅，HR 填原因後轉綠）
                filledClockOut.Add(date);
            }

            // ③ 補加班結束卡：已取消（2026-10）。加班給付依實際打卡結算，沒打結束卡＝結算 0，
            //    補成「開始 + 預估時數」等於替人憑空創造加班時數。
        }

        return new AutoClockResult(
            filledClockIn.Count  == 0 ? null : new AutoClockInInfo(filledClockIn.Count, ToDateStrings(filledClockIn)),
            filledClockOut.Count == 0 ? null : new AutoClockOutInfo(filledClockOut.Count, ToDateStrings(filledClockOut)),
            null);   // AutoOvertimeEnd：不再補加班結束卡，欄位保留（AuthHandler 回應結構不變）
    }

    /// <summary>該列有下班卡或加班卡、卻沒有上班卡 —— 人確實來過，只是漏打上班</summary>
    private static bool NeedsClockIn(AttendanceRecord a) =>
        a.ClockInTime is null && (a.ClockOutTime is not null || a.OvertimeStartTime is not null);

    /// <summary>
    /// 把該員工與目標日期有交集的已核准請假逐日展開（扣掉已核准銷假日），依日期分組。
    /// 工作日判定一律以「假單所有人」解析，此處呼叫者即本人。
    /// </summary>
    private static async Task<Dictionary<DateTime, List<LeaveDay>>> ExpandLeavesAsync(
        AppDbContext db, EmployeeWorkdays workdays, User user, HashSet<DateTime> targetDates)
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
                workdays, leave.LeaveType, leave.StartDate, leave.EndDate);

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
