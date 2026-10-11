using System.Security.Claims;
using Jabez.Api.Data;
using Jabez.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Common;

/// <summary>
/// 申請單「單筆詳情」的檢視授權單一真相（申請人以外的人能不能讀這張單）。
///
/// 起因：簽核作業詳情頁的列印按鈕（2026-09 移到頁首、各階段皆可按）是走**申請單本身**的
/// <c>GET /{type}-requests/{id}</c> 取 PDF 原料，但該端點原本只放行「申請人本人 / Superadmin」，
/// 於是審核者一按列印就拿到 404 →「載入 XX 申請資料失敗，無法匯出 PDF」。
/// 五個同類 handler 當時各寫各的（travel / travel_payment 只認申請人、write_off 系列認「已審 ∪ 指定審核」、
/// advance 完全不判），故收斂於此。
///
/// 判準刻意與 <c>GET /approval-tasks/{appType}/{id}</c> 的存取控制一致 ——
/// 能開簽核詳情頁的人本來就看得到整張單的內容（明細、金額、指定審核者），
/// 故這裡不是放寬授權面，只是讓兩條取得同一份資料的路徑對齊。
/// </summary>
public static class RequestViewAccess
{
    /// <summary>
    /// 呼叫者是否可檢視此申請單：
    /// 申請人本人 ∪ Superadmin ∪ 持 approval-tasks:read（＝簽核台可見者）
    /// ∪ 曾審核過（ApprovalRecord）∪ 被指定為審核者 ∪ 被指派升級審核。
    /// </summary>
    /// <param name="applicationType">多型足跡表的申請類型（travel / holiday_travel / write_off …），須與送簽時寫入的值一致。</param>
    /// <param name="isApplicant">呼叫者是否為申請人本人（各表欄位不同，由呼叫端判定後傳入）。</param>
    /// <param name="isStepReviewer">
    /// 選填：呼叫者是否為此單「目前可簽核者」（固定池關卡尚未簽、也非指定 / 升級者，簽核足跡查不到）。
    /// 僅 ApprovalTaskHandler 有 ApprovalFlowService 可算；提供時，持 approval-tasks:read 但申請人不在部門範圍內者仍可憑此放行。
    /// </param>
    public static async Task<bool> CanViewAsync(
        AppDbContext db, ClaimsPrincipal? principal, Guid callerId, string applicationType, int requestId, bool isApplicant,
        Func<Task<bool>>? isStepReviewer = null)
    {
        if (isApplicant)
            return true;

        if (string.Equals(principal?.FindFirst("is_superadmin")?.Value, "true", StringComparison.OrdinalIgnoreCase))
            return true;

        // approval-tasks:read 只代表「進得了簽核台」，不代表能讀全公司任何一張單（逐一試 id 即可外洩）。
        // 還須申請人部門落在呼叫者的部門可見性範圍（ProjectAccessScope）內。
        if (principal?.FindAll("permissions").Any(c => c.Value == PermissionCodes.ApprovalTasksRead) == true
            && principal is not null)
        {
            var scope = await new ProjectAccessResolver(db).ResolveAsync(principal);
            if (scope.SeeAll)
                return true;

            var applicantDeptId = await GetApplicantDepartmentIdAsync(db, applicationType, requestId);
            if (applicantDeptId is int dept && scope.AllowedDepartmentIds.Contains(dept))
                return true;

            if (isStepReviewer is not null && await isStepReviewer())
                return true;
        }

        if (await db.ApprovalRecords.AsNoTracking()
                .AnyAsync(ar => ar.ApplicationType == applicationType && ar.ApplicationId == requestId && ar.ReviewedById == callerId))
            return true;

        if (await db.RequestDesignatedReviewers.AsNoTracking()
                .AnyAsync(r => r.RequestType == applicationType && r.RequestId == requestId && r.ReviewerId == callerId))
            return true;

        return await db.EscalationOverrides.AsNoTracking()
            .AnyAsync(e => e.ApplicationType == applicationType && e.ApplicationId == requestId && e.ReviewerId == callerId);
    }

    /// <summary>
    /// 組出 <see cref="CanViewAsync"/> 的 <c>isStepReviewer</c> callback（2026-10）：呼叫者是否為此單「目前候選審核者」
    /// （固定池關卡尚未簽、也非指定 / 升級者，簽核足跡查不到）。僅 pending 單才解析。
    /// 供 advance / travel / travel_payment / write_off / travel_write_off / shift_change 的單筆詳情端點使用，
    /// 與 ApprovalTaskHandler.GetByIdAsync 同一判準 —— 否則申請人不在其部門範圍的待審者按列印 PDF 會收到 404。
    /// </summary>
    public static Func<Task<bool>> StepReviewerProbe(
        AppDbContext db, IApprovalFlowService flow, string applicationType, int requestId, Guid callerId)
        => async () =>
        {
            (string Status, int? ItemId, Guid? ApplicantId)? row = applicationType switch
            {
                "advance" => await db.AdvanceRequests.AsNoTracking().Where(x => x.Id == requestId)
                    .Select(x => new ValueTuple<string, int?, Guid?>(x.ApprovalStatus, x.ApprovalItemId, x.SubmittedById)).FirstOrDefaultAsync(),
                "write_off" => await db.WriteOffRecords.AsNoTracking().Where(x => x.Id == requestId)
                    .Select(x => new ValueTuple<string, int?, Guid?>(x.ApprovalStatus, x.ApprovalItemId, x.SubmittedById)).FirstOrDefaultAsync(),
                "travel_write_off" => await db.TravelWriteOffRecords.AsNoTracking().Where(x => x.Id == requestId)
                    .Select(x => new ValueTuple<string, int?, Guid?>(x.ApprovalStatus, x.ApprovalItemId, x.SubmittedById)).FirstOrDefaultAsync(),
                "travel" or "holiday_travel" => await db.TravelRequests.AsNoTracking().Where(x => x.Id == requestId)
                    .Select(x => new ValueTuple<string, int?, Guid?>(x.ApprovalStatus, x.ApprovalItemId, x.EmployeeId)).FirstOrDefaultAsync(),
                "travel_payment" => await db.TravelPaymentRequests.AsNoTracking().Where(x => x.Id == requestId)
                    .Select(x => new ValueTuple<string, int?, Guid?>(x.ApprovalStatus, x.ApprovalItemId, x.EmployeeId)).FirstOrDefaultAsync(),
                "shift_change" => await db.ShiftChangeRequests.AsNoTracking().Where(x => x.Id == requestId)
                    .Select(x => new ValueTuple<string, int?, Guid?>(x.ApprovalStatus, x.ApprovalItemId, x.EmployeeId)).FirstOrDefaultAsync(),
                _ => null,
            };
            if (row is null || row.Value.Status != "pending" || row.Value.ApplicantId is null) return false;

            var reviewers = await flow.ResolveStepReviewersAsync(applicationType, requestId, row.Value.ItemId, row.Value.ApplicantId.Value);
            return reviewers.Any(sr => sr.Reviewers.Any(r => r.Id == callerId));
        };

    /// <summary>申請單申請人的所屬部門（欄位差異同 ApprovalTaskHandler.GetApplicantIdAsync）；查無則 null。</summary>
    private static async Task<int?> GetApplicantDepartmentIdAsync(AppDbContext db, string applicationType, int id)
    {
        var applicantId = applicationType switch
        {
            "payment_request"            => await db.PaymentRequests.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.SubmittedById).FirstOrDefaultAsync(),
            "leave"                      => await db.LeaveRequests.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.EmployeeId).FirstOrDefaultAsync(),
            "leave_revocation"           => await db.LeaveRevocations.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.EmployeeId).FirstOrDefaultAsync(),
            "shift_change"               => await db.ShiftChangeRequests.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.EmployeeId).FirstOrDefaultAsync(),
            "travel" or "holiday_travel" => await db.TravelRequests.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.EmployeeId).FirstOrDefaultAsync(),
            "overtime"                   => await db.OvertimeRequests.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.EmployeeId).FirstOrDefaultAsync(),
            "advance"                    => await db.AdvanceRequests.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.SubmittedById).FirstOrDefaultAsync(),
            "write_off"                  => await db.WriteOffRecords.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.SubmittedById).FirstOrDefaultAsync(),
            "travel_write_off"           => await db.TravelWriteOffRecords.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.SubmittedById).FirstOrDefaultAsync(),
            "travel_payment"             => await db.TravelPaymentRequests.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.EmployeeId).FirstOrDefaultAsync(),
            "pre_review"                 => await db.PreReviewRequests.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.SubmittedById).FirstOrDefaultAsync(),
            _                            => null,
        };
        if (applicantId is null) return null;
        return await db.Users.AsNoTracking().Where(u => u.Id == applicantId).Select(u => u.DepartmentId).FirstOrDefaultAsync();
    }
}
