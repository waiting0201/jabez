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
///
/// 【例外指定審核（ApprovalStepException）的兩個真相】—— 以時間軸切分：
///   送單前 / 送單當下 → 查例外表：<see cref="GetEffectiveDesignatedStepOrdersAsync"/>
///     （消費點僅 2 處：GET /approval-items/active、<see cref="ValidateAndNormalizeAsync"/>）
///   送單完成後      → 看 designee 快照：<see cref="EffectiveDesignatedStepOrders"/>
///     （designee 資料列本身即「申請當下例外命中」的證據，故管理者事後改名單不影響在飛行中的申請）
/// </summary>
public static class DesignatedReviewerHelper
{
    /// <summary>
    /// 【送單前 / 送單當下】「對此申請人而言」的有效指定審核步驟 StepOrder 集合
    /// ＝ UseApplicantDesignated=true 的步驟 ∪ 例外名單含此申請人的步驟。
    /// 此時 RequestDesignatedReviewers 尚未定案，只能查 ApprovalStepException 表。
    /// </summary>
    public static async Task<HashSet<int>> GetEffectiveDesignatedStepOrdersAsync(
        AppDbContext db, int approvalItemId, Guid applicantId)
    {
        var orders = await db.ApprovalSteps
            .AsNoTracking()
            .Where(s => s.ApprovalItemId == approvalItemId
                && (s.UseApplicantDesignated || s.Exceptions.Any(e => e.UserId == applicantId)))
            .Select(s => s.StepOrder)
            .ToListAsync();
        return [.. orders];
    }

    /// <summary>
    /// 【送單完成後】有效指定審核步驟（純記憶體，不查 DB）：
    /// 原生 UseApplicantDesignated 步驟 ∪ 已有 designee 綁定（ApprovalStepOrder）的步驟。
    /// designee 列由送單當下的例外判定寫入並經 ValidateAndNormalizeAsync 剔除非法綁定，
    /// 故此處等同「申請當下的例外快照」。
    /// </summary>
    public static HashSet<int> EffectiveDesignatedStepOrders(
        IEnumerable<ApprovalStep> steps,
        IReadOnlyList<DesignatedReviewerRequest>? designatedReviewers)
    {
        var bound = designatedReviewers?.Select(r => r.ApprovalStepOrder).ToHashSet() ?? [];
        return [.. steps
            .Where(s => s.UseApplicantDesignated || bound.Contains(s.StepOrder))
            .Select(s => s.StepOrder)];
    }

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
    /// 3. 剔除綁在「非有效指定步驟」上的 designee（防提權：否則 client 可用 approvalStepOrder 劫持固定部門步驟）。
    /// 4. 每個 designated step 至少要有一位 designee，否則報錯（取代各 handler 既有守門）。
    /// 有效指定步驟＝原生 UseApplicantDesignated ∪ 例外名單命中 applicantId（見 GetEffectiveDesignatedStepOrdersAsync）。
    /// 變更已寫入 ChangeTracker，呼叫端隨後 SaveChanges 即可。
    /// </summary>
    public static async Task ValidateAndNormalizeAsync(
        AppDbContext db, string requestType, int requestId, int? approvalItemId, Guid applicantId)
    {
        if (approvalItemId is null)
            return;

        // 送單當下真相：原生指定步驟 ∪ 此申請人命中的例外步驟
        var designatedSet = await GetEffectiveDesignatedStepOrdersAsync(db, approvalItemId.Value, applicantId);
        var designatedStepOrders = designatedSet.OrderBy(o => o).ToList();

        if (designatedStepOrders.Count == 0)
        {
            // 此流程對本申請人無指定審核步驟 → 殘留的 designee（例如草稿期間換過流程／部門）一律清掉
            var stale = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == requestType && r.RequestId == requestId)
                .ToListAsync();
            if (stale.Count > 0)
                db.RequestDesignatedReviewers.RemoveRange(stale);
            return;
        }

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

        // 剔除綁在「非有效指定步驟」上的 designee。
        // 送單後的真相是 designee 快照，若不剔除，惡意/錯誤 client 可送 ApprovalStepOrder=N（N 其實是固定部門步驟）
        // 把該步驟劫持成自己挑的人審核。採靜默剔除而非丟 400：草稿期間申請人可能調部門而換到別條流程，
        // 丟錯會誤傷正常使用者。
        var illegal = designees.Where(d => !designatedSet.Contains(d.ApprovalStepOrder)).ToList();
        if (illegal.Count > 0)
        {
            db.RequestDesignatedReviewers.RemoveRange(illegal);
            designees = designees.Except(illegal).ToList();
        }

        // 例外指定審核的限定職稱：designee 的職稱必須在該步驟的限定名單內。
        // 與 :138 的靜默剔除刻意不同 —— 那是「此步驟對我已非指定步驟」的殘留（使用者無從得知也無從修正）；
        // 職稱不符則是使用者挑錯人、可自行修正，且靜默剔除會退化成下方「請提供指定審核者」的誤導訊息。
        // 須在正規化（補齊 ApprovalStepOrder==0）與剔除非法綁定之後才判定。
        await ValidateDesignatedJobTitlesAsync(db, approvalItemId.Value, applicantId, designees);

        // 被抑制的指定步驟（首個指定步驟＝所選部門最高職稱 → 其後指定步驟不需選人）不列入必填檢查。
        // 注意：須在上方正規化（補齊 ApprovalStepOrder==0）之後才判定，否則第一步首位 designee 綁定抓不到。
        var normalized = designees
            .Select(d => new DesignatedReviewerRequest(d.ReviewerId, d.StepOrder, d.ApprovalStepOrder, d.SelectedDepartmentId))
            .ToList();
        var suppressed = await GetSuppressedDesignatedStepOrdersAsync(db, approvalItemId.Value, normalized, designatedSet);

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
    /// 驗證「例外指定審核限定職稱」：只有例外命中且有設限定職稱的步驟才檢查，
    /// 該步驟的 designee 職稱須落在限定名單內，否則丟 400（訊息指名步驟）。
    /// 限定職稱僅存在於例外步驟（由 ApprovalHandler 守門），故此處以「例外命中 + 有名單」為條件即足夠。
    /// </summary>
    private static async Task ValidateDesignatedJobTitlesAsync(
        AppDbContext db, int approvalItemId, Guid applicantId,
        IReadOnlyList<RequestDesignatedReviewer> designees)
    {
        if (designees.Count == 0) return;

        var limits = await db.ApprovalSteps
            .AsNoTracking()
            .Where(s => s.ApprovalItemId == approvalItemId
                && s.Exceptions.Any(e => e.UserId == applicantId)
                && s.DesignatedJobTitles.Any())
            .Select(s => new { s.StepOrder, JobTitleIds = s.DesignatedJobTitles.Select(j => j.JobTitleId).ToList() })
            .ToListAsync();
        if (limits.Count == 0) return;

        var limitMap = limits.ToDictionary(x => x.StepOrder, x => x.JobTitleIds);

        var reviewerIds = designees.Select(d => d.ReviewerId).Distinct().ToList();
        var reviewerJobTitles = await db.Users
            .AsNoTracking()
            .Where(u => reviewerIds.Contains(u.Id))
            .Select(u => new { u.Id, u.JobTitleId })
            .ToDictionaryAsync(u => u.Id, u => u.JobTitleId);

        foreach (var d in designees)
        {
            if (!limitMap.TryGetValue(d.ApprovalStepOrder, out var allowed)) continue;
            reviewerJobTitles.TryGetValue(d.ReviewerId, out var jobTitleId);
            if (jobTitleId is null || !allowed.Contains(jobTitleId.Value))
                throw AppException.BadRequest($"步驟 {d.ApprovalStepOrder} 的指定審核者職稱不符限定職稱，請重新選擇。");
        }
    }

    /// <summary>
    /// 回傳「被抑制的指定審核步驟 StepOrder 集合」。
    /// designatedStepOrders 為「對此申請人的有效指定步驟集合」（含例外命中），由呼叫端依所處時間軸決定來源。
    /// 條件：第一個有效指定步驟為 DesignatedRequiresDepartment=true，
    /// 該步驟所選部門屬於 DepartmentCodes.DesignatedTopLevelSuppression（僅
    /// Operations Department / Brand Department(疆界地域美學) 適用此規則，2026-07 限定），
    /// 且該步驟首位 designee（min StepOrder）＝其 SelectedDepartmentId 部門中
    /// active、非 superadmin、有職稱者的最高職稱（min JobTitle.Level）本人。
    /// 成立 → 回傳「第一個指定步驟之後的所有指定步驟 StepOrder」；不成立 → 空集合。
    /// 為送單驗證與簽核解析（ResolveStartingStepAsync / SkipUnreviewableStepsAsync）共用的單一真相。
    /// 註：判定池以 Status=="active" 較嚴謹（既有 FindNthSuperiorLevel 僅濾 !IsSuperAdmin）；
    /// 抑制只會讓後續步驟被乾淨跳過，不影響其他授權判斷。
    /// </summary>
    public static async Task<HashSet<int>> GetSuppressedDesignatedStepOrdersAsync(
        AppDbContext db, int approvalItemId,
        IReadOnlyList<DesignatedReviewerRequest> designatedReviewers,
        IReadOnlySet<int> designatedStepOrders)
    {
        var designatedSteps = await db.ApprovalSteps
            .AsNoTracking()
            .Where(s => s.ApprovalItemId == approvalItemId && designatedStepOrders.Contains(s.StepOrder))
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

        // 僅限定部門（Operations Department / Brand Department(疆界地域美學)）才適用自動略過；
        // 其餘部門一律不抑制，維持申請人逐一指定所有指定審核步驟。
        var deptCode = await db.Departments.AsNoTracking()
            .Where(d => d.Id == deptId)
            .Select(d => d.Code)
            .FirstOrDefaultAsync();
        if (deptCode is null || !DepartmentCodes.DesignatedTopLevelSuppression.Contains(deptCode))
            return [];

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
