using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 銷假共用邏輯（靜態，比照 AdvanceSupplementService 慣例：不呼叫 SaveChanges，交易邊界交給呼叫端）。
/// </summary>
public static class LeaveRevocationService
{
    /// <summary>簽核任務 / 簽核紀錄使用的 applicationType（與請假單的 "leave" 隔離，避免同 Id 撞號）</summary>
    public const string AppType = "leave_revocation";

    /// <summary>
    /// 套用一張已核准的銷假單到父請假單。
    ///
    /// 從「該假單所有 ApprovalStatus='approved' 銷假單的 distinct 日期」整組重算，而非 Hours -= X：
    /// 兩張銷假單搶同一天、或同一張被重複套用時結果都會收斂，天然冪等且併發安全。
    /// 呼叫端負責 SaveChangesAsync。
    /// </summary>
    public static async Task ApplyAsync(AppDbContext db, ICalendarDayReadService calendarReader, LeaveRevocation revocation)
    {
        var leave = await db.LeaveRequests.FirstOrDefaultAsync(l => l.Id == revocation.LeaveRequestId);
        if (leave is null) return;

        // 首次銷假時保存原始時數，供顯示「原 40h / 已銷 8h」
        leave.OriginalHours ??= leave.Hours;

        // 本張銷假單此刻的 ApprovalStatus="approved" 尚在 ChangeTracker、還沒進 DB，
        // 查詢撈不到，故明確併入自己的日期（同一批次內重複套用仍收斂，因為是取聯集）
        var revokedDates = await GetApprovedRevokedDatesAsync(db, leave.Id);
        var ownDates = await db.LeaveRevocationDates
            .AsNoTracking()
            .Where(d => d.LeaveRevocationId == revocation.Id)
            .Select(d => d.Date)
            .ToListAsync();
        foreach (var d in ownDates) revokedDates.Add(d.Date);

        var allDays   = await LeaveDayExpander.ExpandAsync(calendarReader, leave);
        var remaining = allDays.Where(d => !revokedDates.Contains(d.Date.Date)).ToList();

        leave.Hours = remaining.Sum(d => d.Hours);

        // 全數銷完 → 終止狀態，自此不再落入任何 approved 查詢（打卡 / 額度 / 重疊 / 扣薪）
        if (remaining.Count == 0)
            leave.ApprovalStatus = "cancelled";
    }

    /// <summary>
    /// 該請假單已被核准銷假的日期集合。
    /// 下游「某日是否仍在請假中」的 EF 版判定與 Apply 重算共用同一份定義。
    /// </summary>
    public static async Task<HashSet<DateTime>> GetApprovedRevokedDatesAsync(AppDbContext db, int leaveRequestId)
    {
        var dates = await db.LeaveRevocationDates
            .AsNoTracking()
            .Where(d => d.LeaveRevocation!.LeaveRequestId == leaveRequestId
                     && d.LeaveRevocation.ApprovalStatus == "approved")
            .Select(d => d.Date)
            .ToListAsync();
        return [.. dates.Select(d => d.Date)];
    }

    /// <summary>
    /// 「該假單此日尚未被核准銷假」的共用 SQL 片段。
    /// 消費點：打卡阻擋 / 休假日免下班卡 / 出缺勤報表 / 打卡提醒 / 重疊驗證。
    /// 使用時 lr 需為 LeaveRequests 的別名，並自行代入要比對的日期運算式。
    /// </summary>
    public static string NotRevokedClause(string leaveAlias, string dateExpr) => $"""
        NOT EXISTS (
            SELECT 1 FROM LeaveRevocationDates rvd
            JOIN LeaveRevocations rv ON rv.Id = rvd.LeaveRevocationId
            WHERE rv.LeaveRequestId = {leaveAlias}.Id
              AND rv.ApprovalStatus = 'approved'
              AND rvd.Date = {dateExpr})
        """;
}
