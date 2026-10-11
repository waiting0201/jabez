using Jabez.Api.Common;
using Jabez.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 加班申請的送件檢查（2026-10 防灌工時）。<c>OvertimeRequestHandler</c> 的 Create / Update / Submit 共用，
/// 核准時另呼叫 <see cref="EnsureMonthlyLimitOnApprovalAsync"/> 再檢一次每月上限。
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

        var others = await SumMonthAsync(db, ownerId, overtimeDate, excludeId, includePending: true);
        ThrowIfOver(limit.Value, others, thisHours, overtimeDate);
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

        var approvedOthers = await SumMonthAsync(db, ot.EmployeeId.Value, ot.OvertimeDate, ot.Id, includePending: false);
        ThrowIfOver(limit.Value, approvedOthers, ot.EstimatedHours, ot.OvertimeDate);
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

    // ── 內部 ────────────────────────────────────────────────────────────────

    private static async Task<int?> GetLimitAsync(AppDbContext db)
    {
        var limit = await db.SystemSettings.AsNoTracking()
            .OrderBy(s => s.Id)
            .Select(s => (int?)s.MonthlyOvertimeLimit)
            .FirstOrDefaultAsync();
        return limit is > 0 ? limit : null;
    }

    private static async Task<decimal> SumMonthAsync(
        AppDbContext db, Guid ownerId, DateTime overtimeDate, int? excludeId, bool includePending)
    {
        var monthStart = new DateTime(overtimeDate.Year, overtimeDate.Month, 1);
        var nextMonth  = monthStart.AddMonths(1);

        return await db.OvertimeRequests.AsNoTracking()
            .Where(o => o.EmployeeId == ownerId
                     && o.OvertimeDate >= monthStart && o.OvertimeDate < nextMonth
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

    private static string StatusZh(string status) => status switch
    {
        "draft"    => "草稿",
        "pending"  => "簽核中",
        "returned" => "退回修改中",
        "approved" => "已核准",
        _          => status,
    };
}
