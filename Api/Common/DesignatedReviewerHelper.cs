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

        // 被抑制的指定步驟（首個指定步驟＝所選部門最高職稱 → 其後指定步驟不需選人）不列入必填檢查。
        // 注意：須在上方正規化（補齊 ApprovalStepOrder==0）之後才判定，否則第一步首位 designee 綁定抓不到。
        var normalized = designees
            .Select(d => new DesignatedReviewerRequest(d.ReviewerId, d.StepOrder, d.ApprovalStepOrder, d.SelectedDepartmentId))
            .ToList();
        var suppressed = await GetSuppressedDesignatedStepOrdersAsync(db, approvalItemId.Value, normalized);

        // 每個 designated step 至少要有一位 designee（被抑制者除外）
        foreach (var stepOrder in designatedStepOrders)
        {
            if (suppressed.Contains(stepOrder))
                continue;
            if (!designees.Any(d => d.ApprovalStepOrder == stepOrder))
                throw AppException.BadRequest("此簽核流程包含申請人指定審核步驟，請提供指定審核者。");
        }
    }

    /// <summary>
    /// 回傳「被抑制的指定審核步驟 StepOrder 集合」。
    /// 條件：第一個 UseApplicantDesignated 步驟為 DesignatedRequiresDepartment=true，
    /// 且該步驟首位 designee（min StepOrder）＝其 SelectedDepartmentId 部門中
    /// active、非 superadmin、有職稱者的最高職稱（min JobTitle.Level）本人。
    /// 成立 → 回傳「第一個指定步驟之後的所有指定步驟 StepOrder」；不成立 → 空集合。
    /// 為送單驗證與簽核解析（ResolveStartingStepAsync / SkipUnreviewableStepsAsync）共用的單一真相。
    /// 註：判定池以 Status=="active" 較嚴謹（既有 FindNthSuperiorLevel 僅濾 !IsSuperAdmin）；
    /// 抑制只會讓後續步驟被乾淨跳過，不影響其他授權判斷。
    /// </summary>
    public static async Task<HashSet<int>> GetSuppressedDesignatedStepOrdersAsync(
        AppDbContext db, int approvalItemId,
        IReadOnlyList<DesignatedReviewerRequest> designatedReviewers)
    {
        var designatedSteps = await db.ApprovalSteps
            .AsNoTracking()
            .Where(s => s.ApprovalItemId == approvalItemId && s.UseApplicantDesignated)
            .OrderBy(s => s.StepOrder)
            .Select(s => new { s.StepOrder, s.DesignatedRequiresDepartment })
            .ToListAsync();

        // 沒有「之後的步驟」可抑制，或第一步非「先選部門」模式 → 不抑制
        if (designatedSteps.Count < 2) return [];
        var first = designatedSteps[0];
        if (!first.DesignatedRequiresDepartment) return [];

        // 第一步首位 designee（min StepOrder）；缺人或無部門 → 保守不抑制
        var firstDesignee = designatedReviewers
            .Where(r => r.ApprovalStepOrder == first.StepOrder)
            .OrderBy(r => r.StepOrder)
            .FirstOrDefault();
        if (firstDesignee is null || firstDesignee.SelectedDepartmentId is null) return [];

        var deptId = firstDesignee.SelectedDepartmentId.Value;

        // 該部門 active、非 superadmin、有職稱者的最高職稱 Level（min）；空池得 null
        var deptMinLevel = await db.Users.AsNoTracking()
            .Where(u => u.DepartmentId == deptId
                && u.Status == "active"
                && !u.IsSuperAdmin
                && u.JobTitle != null)
            .Select(u => (int?)u.JobTitle!.Level)
            .MinAsync();
        if (deptMinLevel is null) return [];

        // 被指定者本人：須 active、非 superadmin、確實在該部門、職稱 Level 等於部門最高
        var reviewer = await db.Users.AsNoTracking()
            .Include(u => u.JobTitle)
            .FirstOrDefaultAsync(u => u.Id == firstDesignee.ReviewerId);

        bool isTopOfDept =
            reviewer is not null
            && reviewer.Status == "active"
            && !reviewer.IsSuperAdmin
            && reviewer.DepartmentId == deptId
            && reviewer.JobTitle is not null
            && reviewer.JobTitle.Level == deptMinLevel.Value;
        if (!isTopOfDept) return [];

        return designatedSteps.Skip(1).Select(s => s.StepOrder).ToHashSet();
    }
}
