using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 加班申請的送件檢查（2026-10 防灌工時）。<c>OvertimeRequestHandler</c> 的 Create / Update / Submit 共用，
/// 核准時另呼叫 <see cref="EnsureMonthlyLimitOnApprovalAsync"/> / <see cref="EnsureQuarterlyLimitOnApprovalAsync"/> 再檢一次每月 / 每期（三個月）上限。
/// 靜態、不呼叫 SaveChanges；違規一律丟 <see cref="AppException.BadRequest"/>。
///
/// 單日時數上限（上班日 4h / 假日 12h）不在這裡 —— 它依日別而定，仍由 Handler 的
/// <c>GuardOvertimeHoursAsync</c>（<see cref="OvertimePayCalculator.ExceedsCap"/>）負責，且不分補休 / 加班費。
/// </summary>
public static class OvertimeRequestGuard
{
    /// <summary>補登期限：加班日期最早只能是「今天往前 N 天」。</summary>
    public const int MaxBackdateDays = 7;

    /// <summary>
    /// 每期（三個月）上限自哪一期起檢查（2026-11 起，勞基法 §32 經勞資會議同意之延長上限）。
    /// 期起日早於此日的期別（如 2026-08～10 月）一律不檢查，避免上線前的加班被追溯擋下。
    /// </summary>
    public static readonly DateTime QuarterlyLimitEffectiveFrom = new(2026, 11, 1);

    /// <summary>
    /// 無薪假別：這些假別全日請假時不領薪，當日仍可能被要求加班，故不擋。
    /// 其餘假別（特休 / 補休 / 公假 / 婚喪 / 產假 / 病假 / 生理假…）一律視為有薪（含半薪）而擋下 ——
    /// 病假與生理假雖僅半薪，仍屬「有領薪的請假日」，同日再領加班費即重複給付。
    /// 與 <c>PayrollReadService</c> 全額扣薪的假別（personal / family_care）及留職停薪（parental_leave*）對齊。
    /// </summary>
    public static readonly HashSet<string> UnpaidLeaveTypes =
        ["personal", "family_care", "parental_leave", "parental_leave_daily"];

    /// <summary>補登期限檢查（以今天為基準，含邊界當天）。</summary>
    public static void EnsureBackdate(DateTime overtimeDate)
    {
        var earliest = Clock.Today.AddDays(-MaxBackdateDays);
        if (overtimeDate.Date < earliest)
            throw AppException.BadRequest(
                $"加班日期最早只能補登到 {MaxBackdateDays} 天內（{earliest:yyyy/MM/dd} 起），請洽主管由出缺勤補正。");
    }

    /// <summary>
    /// 同一人同一日只能有一張加班單（已拒絕者不算）。
    /// <paramref name="excludeId"/>＝正在編輯的單自己（退回修改中的單編輯自己不應被自己擋下）。
    /// </summary>
    public static async Task EnsureNoDuplicateAsync(
        AppDbContext db, Guid ownerId, DateTime overtimeDate, int? excludeId)
    {
        var day = overtimeDate.Date;
        var nextDay = day.AddDays(1);
        // OvertimeDate 欄位為 datetime2（未設定 date 型別），以區間比對而非 == 以免被時間部分影響
        var clash = await db.OvertimeRequests.AsNoTracking()
            .Where(o => o.EmployeeId == ownerId
                     && o.OvertimeDate >= day && o.OvertimeDate < nextDay
                     && o.ApprovalStatus != "rejected"
                     && (excludeId == null || o.Id != excludeId))
            .Select(o => new { o.Id, o.RequestNo, o.ApprovalStatus })
            .FirstOrDefaultAsync();

        if (clash is null) return;

        var label = clash.RequestNo ?? "尚未取號的草稿";
        throw AppException.BadRequest(
            $"{day:yyyy/MM/dd} 已有加班申請單（{label}，狀態：{StatusZh(clash.ApprovalStatus)}），同一日只能申請一張；" +
            "請直接修改該張單，或待其被拒絕後再申請。");
    }

    /// <summary>
    /// 每月上限（SystemSetting.MonthlyOvertimeLimit，null 或 ≤ 0 視為不限制）：
    /// 該加班日所屬月份「已核准 ＋ 簽核中 ＋ 本單」的**申請時數**合計不得超過上限。
    /// 草稿與已拒絕不計入。本單以參數傳入（Create 時尚未有 Id），編輯中的舊單以 <paramref name="excludeId"/> 排除避免重複計算。
    /// </summary>
    public static async Task EnsureMonthlyLimitAsync(
        AppDbContext db, Guid ownerId, DateTime overtimeDate, decimal thisHours, int? excludeId)
    {
        var limit = await GetLimitAsync(db);
        if (limit is null) return;

        var (monthStart, nextMonth) = MonthRange(overtimeDate);
        var others = await SumRangeAsync(db, ownerId, monthStart, nextMonth, excludeId, includePending: true);
        ThrowIfOver(limit.Value, others, thisHours, overtimeDate);
    }

    /// <summary>
    /// 每期（三個月）上限（SystemSetting.QuarterlyOvertimeLimit，≤ 0 視為不限制）：
    /// 期別固定為 11–1 月 / 2–4 月 / 5–7 月 / 8–10 月（見 <see cref="QuarterRange"/>），
    /// 計算口徑同每月上限（已核准 ＋ 簽核中 ＋ 本單的申請時數）。期起日早於 <see cref="QuarterlyLimitEffectiveFrom"/> 不檢查。
    /// </summary>
    public static async Task EnsureQuarterlyLimitAsync(
        AppDbContext db, Guid ownerId, DateTime overtimeDate, decimal thisHours, int? excludeId)
    {
        var (start, end) = QuarterRange(overtimeDate);
        if (start < QuarterlyLimitEffectiveFrom) return;
        var limit = await GetQuarterlyLimitAsync(db);
        if (limit is null) return;

        var others = await SumRangeAsync(db, ownerId, start, end, excludeId, includePending: true);
        ThrowIfQuarterOver(limit.Value, others, thisHours, start, end);
    }

    /// <summary>
    /// 核准時再檢一次。只計「已核准（不含本單）＋ 本單」，**不再把其他簽核中的單算進去**：
    /// 那些單送件時已各自過了送件檢查，若核准時也把它們全算進來，管理者調低上限後會造成
    /// 兩張都過不了的互卡（A 核准要算 B、B 核准要算 A）。
    /// </summary>
    public static async Task EnsureMonthlyLimitOnApprovalAsync(
        AppDbContext db, Models.Entities.OvertimeRequest ot)
    {
        if (ot.EmployeeId is null) return;
        var limit = await GetLimitAsync(db);
        if (limit is null) return;

        var (monthStart, nextMonth) = MonthRange(ot.OvertimeDate);
        var approvedOthers = await SumRangeAsync(db, ot.EmployeeId.Value, monthStart, nextMonth, ot.Id, includePending: false);
        ThrowIfOver(limit.Value, approvedOthers, ot.EstimatedHours, ot.OvertimeDate);
    }

    /// <summary>核准時再檢一次每期上限，口徑同 <see cref="EnsureMonthlyLimitOnApprovalAsync"/>（只計已核准 ＋ 本單）。</summary>
    public static async Task EnsureQuarterlyLimitOnApprovalAsync(
        AppDbContext db, Models.Entities.OvertimeRequest ot)
    {
        if (ot.EmployeeId is null) return;
        var (start, end) = QuarterRange(ot.OvertimeDate);
        if (start < QuarterlyLimitEffectiveFrom) return;
        var limit = await GetQuarterlyLimitAsync(db);
        if (limit is null) return;

        var approvedOthers = await SumRangeAsync(db, ot.EmployeeId.Value, start, end, ot.Id, includePending: false);
        ThrowIfQuarterOver(limit.Value, approvedOthers, ot.EstimatedHours, start, end);
    }

    /// <summary>
    /// 加班日所屬的期別 [期起日, 下一期起日)：期別固定自 2 / 5 / 8 / 11 月 1 日起算，各三個月
    /// （例：2026-12-15 → 2026-11-01 ～ 2027-02-01；2027-01-31 亦屬同一期）。
    /// </summary>
    public static (DateTime Start, DateTime End) QuarterRange(DateTime date)
    {
        var offset = (date.Month + 10) % 3;   // 2/5/8/11 月 → 0，其次一個月 → 1，再次 → 2
        var start  = new DateTime(date.Year, date.Month, 1).AddMonths(-offset);
        return (start, start.AddMonths(3));
    }

    /// <summary>
    /// 當日已有涵蓋全天的已核准**有薪假**時不可申請加班（同一天領薪休假又領加班費 / 補休）。
    /// 全日判定走 <see cref="LeaveDayExpander"/>（與出缺勤、銷假同一份請假日展開，已扣國定假日 / 排班休假日 / 已核准銷假日）：
    /// 當日有「全天」段，或「上午」與「下午」兩段皆被有薪假蓋住（可分屬兩張單）。
    /// 只請半天、或請無薪假（事假 / 家庭照顧假 / 育嬰留停）者不擋。
    /// </summary>
    public static async Task EnsureNotOnPaidFullDayLeaveAsync(
        AppDbContext db, IEmployeeWorkdaysFactory workdaysFactory, Guid ownerId, DateTime overtimeDate)
    {
        var day = overtimeDate.Date;

        var leaves = await db.LeaveRequests.AsNoTracking()
            .Where(l => l.EmployeeId == ownerId
                     && l.ApprovalStatus == "approved"
                     && !UnpaidLeaveTypes.Contains(l.LeaveType)
                     && l.StartDate.Date <= day
                     && l.EndDate.Date >= day)
            .ToListAsync();
        if (leaves.Count == 0) return;

        var workdays = await workdaysFactory.ForAsync(ownerId);
        bool am = false, pm = false;

        foreach (var leave in leaves)
        {
            var revoked = await LeaveRevocationService.GetApprovedRevokedDatesAsync(db, leave.Id);
            if (revoked.Contains(day)) continue;

            foreach (var d in await LeaveDayExpander.ExpandAsync(workdays, leave))
            {
                if (d.Date.Date != day || d.Hours <= 0m) continue;
                switch (d.Segment)
                {
                    case LeaveDaySegments.Full: am = pm = true; break;
                    case LeaveDaySegments.Am:   am = true;      break;
                    case LeaveDaySegments.Pm:   pm = true;      break;
                }
            }
            if (am && pm) break;
        }

        if (am && pm)
            throw AppException.BadRequest(
                $"{day:yyyy/MM/dd} 您已有全天的已核准請假（有薪假），該日不可申請加班。");
    }

    // ── 事後申請（RequestKind = post_hoc，2026-10）─────────────────────────────────

    /// <summary>
    /// 事後申請資格判定（資料面）：須於<b>加班日後一個工作日內</b>提出（截止日＝加班日之後第一個公司工作日 23:59:59，
    /// 遇週六日 / 國定假日順延，見 <see cref="WorkCalendarHelper.NextCompanyWorkingDayAsync"/>）、當日有出缺勤紀錄、
    /// 已<b>親自</b>打下班卡（非系統補卡）、且尚無加班打卡。
    /// <c>GET /overtime-requests/post-hoc-context</c> 與送件驗證共用，避免兩處漂移。回傳的 <c>Record</c> 為 AsNoTracking 快照。
    /// </summary>
    public static async Task<(bool Eligible, string? Reason, Models.Entities.AttendanceRecord? Record, DateTime? Deadline)>
        EvaluatePostHocEligibilityAsync(AppDbContext db, ICalendarDayReadService calendarReader, Guid ownerId, DateTime overtimeDate)
    {
        var day = overtimeDate.Date;
        var today = Clock.Today;
        if (day > today)
            return (false, "加班日期不可晚於今天。", null, null);

        var deadline = await WorkCalendarHelper.NextCompanyWorkingDayAsync(calendarReader, day);
        if (today > deadline)
            return (false,
                $"事後申請須於加班日的下一個工作日（{deadline:yyyy/MM/dd}）23:59 前提出，已逾期；請洽主管由出缺勤補正。",
                null, deadline);

        var record = await db.AttendanceRecords.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == ownerId && a.RecordDate == day);

        if (record is null || record.ClockOutTime is null)
            return (false, $"{day:yyyy/MM/dd} 尚未打下班卡，無法申請事後加班；請改用正常申請，或下班打卡後再申請。", record, deadline);
        if (record.IsClockOutAuto)
            return (false, $"{day:yyyy/MM/dd} 的下班卡為系統補卡（非親自打卡），無法申請事後加班，請洽主管由出缺勤補正。", record, deadline);
        if (record.OvertimeStartTime is not null || record.OvertimeEndTime is not null)
            return (false, $"{day:yyyy/MM/dd} 已有加班打卡紀錄，請改用正常申請（或洽主管由出缺勤補正）。", record, deadline);

        return (true, null, record, deadline);
    }

    /// <summary>
    /// 事後申請送件檢查（Create / Update / Submit 共用，僅 RequestKind = post_hoc 時呼叫）：
    /// 資格（見 <see cref="EvaluatePostHocEligibilityAsync"/>）→ 起訖必填、訖 &gt; 起、起 ≥ 加班日 00:00、
    /// 訖 &lt; 隔日 06:00（<see cref="OvertimeSettlementService.CrossDayEndCutoffHour"/>）、訖 ≤ 現在（不可申報尚未發生的時段）
    /// → 專案時數合計不得超過「起訖時數扣除與正常上班時段重疊」。
    /// <b>刻意不比對下班卡時間</b>（使用者決議：起訖自由填寫，真實性由審核者把關，稽核紀錄可追查）。
    /// </summary>
    public static async Task EnsurePostHocAsync(
        AppDbContext db, ICalendarDayReadService calendarReader, IShiftScheduleReadService shiftSchedule,
        IWorkdayScheduleProvider scheduleProvider,
        Guid ownerId, DateTime overtimeDate, DateTime? actualStart, DateTime? actualEnd, decimal projectHours)
    {
        var (eligible, reason, _, _) = await EvaluatePostHocEligibilityAsync(db, calendarReader, ownerId, overtimeDate);
        if (!eligible) throw AppException.BadRequest(reason!);

        if (actualStart is null || actualEnd is null)
            throw AppException.BadRequest("事後申請須填寫實際加班開始與結束時間。");

        var day = overtimeDate.Date;
        if (actualEnd.Value <= actualStart.Value)
            throw AppException.BadRequest("實際加班結束時間須晚於開始時間。");
        if (actualStart.Value < day)
            throw AppException.BadRequest("實際加班開始時間不可早於加班日 00:00。");
        if (actualEnd.Value >= day.AddDays(1).AddHours(OvertimeSettlementService.CrossDayEndCutoffHour))
            throw AppException.BadRequest(
                $"實際加班結束時間須早於隔日 {OvertimeSettlementService.CrossDayEndCutoffHour:00}:00。");
        // 事後申請只能申報「已發生」的加班：否則當天提早下班後即可預報當晚時段，核准當下就寫入出缺勤並結算
        if (actualEnd.Value > Clock.Now)
            throw AppException.BadRequest("事後申請的實際加班結束時間不可晚於現在（尚未發生的加班請改用正常申請）。");

        var dayType  = await shiftSchedule.ResolveDayTypeAsync(ownerId, day);
        var schedule = await scheduleProvider.ForAsync(day);
        var window   = OvertimeSettlement.RegularWindowFor(day, dayType, schedule);

        // approvedHours 傳入夠大的值，取回的就是「扣除正常上班時段重疊後、捨去到 0.1 小時」的可申請上限
        var maxHours = OvertimeSettlement.ComputeSettled(999m, actualStart, actualEnd, window?.Start, window?.End);
        if (projectHours > maxHours)
            throw AppException.BadRequest(
                $"專案時數合計 {projectHours:0.#} 小時超過實際加班起訖可計的 {maxHours:0.#} 小時" +
                "（起訖時數扣除與正常上班時段重疊的部分）。");
    }

    // ── 內部 ────────────────────────────────────────────────────────────────

    private static async Task<int?> GetLimitAsync(AppDbContext db)
    {
        var limit = await db.SystemSettings.AsNoTracking()
            .OrderBy(s => s.Id)
            .Select(s => (int?)s.MonthlyOvertimeLimit)
            .FirstOrDefaultAsync();
        return limit is > 0 ? limit : null;
    }

    private static async Task<int?> GetQuarterlyLimitAsync(AppDbContext db)
    {
        var limit = await db.SystemSettings.AsNoTracking()
            .OrderBy(s => s.Id)
            .Select(s => (int?)s.QuarterlyOvertimeLimit)
            .FirstOrDefaultAsync();
        return limit is > 0 ? limit : null;
    }

    private static (DateTime Start, DateTime End) MonthRange(DateTime date)
    {
        var start = new DateTime(date.Year, date.Month, 1);
        return (start, start.AddMonths(1));
    }

    /// <summary>[start, end) 區間內該員「已核准（＋ 簽核中）」的申請時數合計，排除 <paramref name="excludeId"/>。</summary>
    private static async Task<decimal> SumRangeAsync(
        AppDbContext db, Guid ownerId, DateTime start, DateTime end, int? excludeId, bool includePending)
    {
        return await db.OvertimeRequests.AsNoTracking()
            .Where(o => o.EmployeeId == ownerId
                     && o.OvertimeDate >= start && o.OvertimeDate < end
                     && (excludeId == null || o.Id != excludeId)
                     && (o.ApprovalStatus == "approved" || (includePending && o.ApprovalStatus == "pending")))
            .SumAsync(o => (decimal?)o.EstimatedHours) ?? 0m;
    }

    private static void ThrowIfOver(int limit, decimal others, decimal thisHours, DateTime date)
    {
        if (others + thisHours <= limit) return;
        throw AppException.BadRequest(
            $"{date:yyyy 年 M 月}加班時數已達每月上限 {limit} 小時（已核准／簽核中 {others:0.#} 小時，加上本單 {thisHours:0.#} 小時），無法申請。");
    }

    private static void ThrowIfQuarterOver(int limit, decimal others, decimal thisHours, DateTime start, DateTime end)
    {
        if (others + thisHours <= limit) return;
        throw AppException.BadRequest(
            $"{start:yyyy/M}～{end.AddMonths(-1):yyyy/M} 這一期（三個月）加班時數已達上限 {limit} 小時" +
            $"（已核准／簽核中 {others:0.#} 小時，加上本單 {thisHours:0.#} 小時），無法申請。");
    }

    private static string StatusZh(string status) => status switch
    {
        "draft"    => "草稿",
        "pending"  => "簽核中",
        "returned" => "退回修改中",
        "approved" => "已核准",
        _          => status,
    };
}
