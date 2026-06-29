using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Common;

/// <summary>
/// 申請人指定審核者（RequestDesignatedReviewer）的共用處理：
/// 一條流程可有多個 UseApplicantDesignated 步驟，每筆 designee 以 ApprovalStepOrder 綁定所屬步驟。
/// 9 種申請類型（payment_request / advance / travel / travel_payment / write_off /
/// travel_write_off / leave / overtime / pre_review）共用本 helper 建立、讀取、驗證與正規化。
/// </summary>
public static class DesignatedReviewerHelper
{
    /// <summary>由請求 DTO 建立待存實體（Create / Update 草稿時用）。</summary>
    public static List<RequestDesignatedReviewer> BuildEntities(
        string requestType, int requestId, IEnumerable<DesignatedReviewerRequest> reqs)
        => reqs
            .OrderBy(r => r.ApprovalStepOrder).ThenBy(r => r.StepOrder)
            .Select(r => new RequestDesignatedReviewer
            {
                RequestType          = requestType,
                RequestId            = requestId,
                ReviewerId           = r.ReviewerId,
                ApprovalStepOrder    = r.ApprovalStepOrder,
                StepOrder            = r.StepOrder,
                SelectedDepartmentId = r.SelectedDepartmentId,
            })
            .ToList();

    /// <summary>
    /// 讀回某申請的所有指定審核者，組成傳給 ResolveStartingStepAsync / SkipUnreviewableStepsAsync 的清單。
    /// 依 (ApprovalStepOrder, StepOrder) 排序，並帶出綁定步驟與選取部門。
    /// </summary>
    public static Task<List<DesignatedReviewerRequest>> ReadForFlowAsync(
        AppDbContext db, string requestType, int requestId)
        => db.RequestDesignatedReviewers
            .AsNoTracking()
            .Where(r => r.RequestType == requestType && r.RequestId == requestId)
            .OrderBy(r => r.ApprovalStepOrder).ThenBy(r => r.StepOrder)
            .Select(r => new DesignatedReviewerRequest(
                r.ReviewerId, r.StepOrder, r.ApprovalStepOrder, r.SelectedDepartmentId))
            .ToListAsync();

    /// <summary>
    /// 送單時正規化 + 驗證指定審核者（此時 ApprovalItemId 已解析）：
    /// 1. 向後相容：designee 的 ApprovalStepOrder 未帶（=0）且流程只有一個 designated step → 自動補成該 step 的 StepOrder。
    /// 2. 流程有 ≥2 個 designated step 卻有未綁定（=0）的 designee → 視為呼叫端 bug，報錯。
    /// 3. 每個 designated step 至少要有一位 designee，否則報錯（取代各 handler 既有守門）。
    /// 變更已寫入 ChangeTracker，呼叫端隨後 SaveChanges 即可。
    /// </summary>
    public static async Task ValidateAndNormalizeAsync(
        AppDbContext db, string requestType, int requestId, int? approvalItemId)
    {
        if (approvalItemId is null)
            return;

        var designatedStepOrders = await db.ApprovalSteps
            .AsNoTracking()
            .Where(s => s.ApprovalItemId == approvalItemId && s.UseApplicantDesignated)
            .Select(s => s.StepOrder)
            .OrderBy(o => o)
            .ToListAsync();

        if (designatedStepOrders.Count == 0)
            return; // 此流程無指定審核步驟

        var designees = await db.RequestDesignatedReviewers
            .Where(r => r.RequestType == requestType && r.RequestId == requestId)
            .ToListAsync();

        // 正規化未綁定（=0）的 designee
        var unbound = designees.Where(d => d.ApprovalStepOrder == 0).ToList();
        if (unbound.Count > 0)
        {
            if (designatedStepOrders.Count == 1)
            {
                foreach (var d in unbound)
                    d.ApprovalStepOrder = designatedStepOrders[0];
            }
            else
            {
                throw AppException.BadRequest("此簽核流程包含多個指定審核步驟，請為每個步驟分別指定審核者。");
            }
        }

        // 每個 designated step 至少要有一位 designee
        foreach (var stepOrder in designatedStepOrders)
        {
            if (!designees.Any(d => d.ApprovalStepOrder == stepOrder))
                throw AppException.BadRequest("此簽核流程包含申請人指定審核步驟，請提供指定審核者。");
        }
    }
}
