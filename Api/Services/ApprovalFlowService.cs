using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 簽核流程輔助服務：送出申請時處理「申請人即審核者」的步驟。
/// 對 payment_request 維持原有自動跳過邏輯；
/// 對 leave / travel / overtime 改為呼叫 EscalationService 升級審核。
/// </summary>
public sealed class ApprovalFlowService(
    AppDbContext db,
    IEscalationService escalationService) : IApprovalFlowService
{
    public async Task<(int startStep, bool autoApproved, EscalationResult? escalation)>
        ResolveStartingStepAsync(int? approvalItemId, Guid applicantId, string applicationType,
            IReadOnlyList<DesignatedReviewerRequest>? designatedReviewers = null)
    {
        if (approvalItemId is null)
            return (1, false, null);

        var steps = await db.ApprovalSteps
            .AsNoTracking()
            .Where(s => s.ApprovalItemId == approvalItemId)
            .OrderBy(s => s.StepOrder)
            .ToListAsync();

        if (steps.Count == 0)
            return (1, false, null);

        var applicant = await db.Users.AsNoTracking()
            .Include(u => u.JobTitle)
            .FirstOrDefaultAsync(u => u.Id == applicantId);
        if (applicant is null)
            return (1, false, null);

        int currentStep = 1;
        int directSupervisorRank = 0; // 追蹤目前是第幾個上層級步驟（0-based）
        foreach (var step in steps)
        {
            // ── UseApplicantDesignated 模式：審核者是申請人指定的人 ──
            if (step.UseApplicantDesignated)
            {
                // 取第一位指定審核者（min StepOrder）的 ReviewerId 進行判斷
                var firstReviewer = designatedReviewers?
                    .OrderBy(r => r.StepOrder)
                    .FirstOrDefault();

                // 自審規則分兩組：
                //   Group A 全程禁止自審（任一位置為申請人即報錯）：leave / travel / overtime / travel_payment
                //   Group B 首位跳過（申請人排第 1 位 → 自動跳過此步驟；2+ 位置不檢查）：payment_request / advance / write_off / travel_write_off / holiday_travel
                // 此處先處理 Group A：當 applicationType 不在 Group B 名單內 → 套用 Group A 規則
                if (applicationType is not ("payment_request" or "advance" or "write_off" or "travel_write_off" or "holiday_travel"))
                {
                    bool anyIsSelf = designatedReviewers?.Any(r => r.ReviewerId == applicantId) ?? false;
                    if (anyIsSelf)
                        throw AppException.BadRequest("指定審核者不能是申請人本人。");
                }

                if (firstReviewer is not null && firstReviewer.ReviewerId != applicantId)
                {
                    // 有指定審核者且第 1 位不是自己 → 從這步開始
                    return (currentStep, false, null);
                }
                else if (firstReviewer is not null && firstReviewer.ReviewerId == applicantId)
                {
                    // payment_request / advance 自審第 1 位 → 跳過此步驟
                    currentStep++;
                    continue;
                }
                else
                {
                    // designatedReviewers 為 null 或空：理論上 Handler 層守門會先擋下，
                    // 此處作 defense-in-depth — 流程明確要求指定審核者卻沒提供，視為呼叫端 bug。
                    // 與 8 個 SubmitAsync Handler 的守門訊息保持一致。
                    throw AppException.BadRequest("此簽核流程包含申請人指定審核步驟，請提供指定審核者。");
                }
            }

            // ── UseDirectSupervisor 模式：找同部門中第 N 層上級 ──
            if (step.UseDirectSupervisor)
            {
                var targetLevel = await FindNthSuperiorLevelAsync(applicant, directSupervisorRank);
                directSupervisorRank++;
                if (targetLevel.HasValue)
                    return (currentStep, false, null); // 有上層級 → 從這步開始
                // 找不到 → 跳過此步驟
                currentStep++;
                continue;
            }

            bool isSelfReview = IsApplicantTheReviewer(step, applicant);

            if (!isSelfReview)
            {
                // 請款/預支/沖銷：若步驟使用申請人部門但該部門無符合條件的審核者，也跳過
                if (applicationType is "payment_request" or "advance" or "write_off" or "travel_write_off" && step.UseApplicantDepartment && applicant.DepartmentId.HasValue)
                {
                    bool hasReviewer = await db.Users.AsNoTracking().AnyAsync(u =>
                        u.DepartmentId == applicant.DepartmentId
                        && u.Id != applicantId
                        && !u.IsSuperAdmin
                        && (step.JobTitleId == null || u.JobTitleId == step.JobTitleId));

                    if (!hasReviewer)
                    {
                        // 該部門找不到審核者 → 跳過此步驟
                        currentStep++;
                        continue;
                    }
                }

                return (currentStep, false, null); // 這一步不是自己且有審核者 → 從這步開始
            }

            // 自審情境：根據申請類型決定處理方式
            if (applicationType is "leave" or "travel" or "overtime")
            {
                // 嘗試升級審核（會拋出 AppException 如果找不到人）
                var escalation = await escalationService.TryEscalateAsync(step, applicant, applicationType);
                if (escalation is not null)
                    return (currentStep, false, escalation);
            }

            // payment_request 或 TryEscalate 回傳 null → 維持原有自動跳過邏輯
            currentStep++;
        }

        // 所有步驟都被跳過 → 自動核准
        return (steps.Count, true, null);
    }

    /// <summary>
    /// 找同部門中第 N 層上級的 Level 值（0-based：rank=0 最接近，rank=1 再上一層...）。
    /// Level 數字越小 = 層級越高。回傳 null 表示找不到。
    /// </summary>
    private async Task<int?> FindNthSuperiorLevelAsync(Models.Entities.User applicant, int rank)
    {
        if (applicant.DepartmentId is null || applicant.JobTitle is null)
            return null;

        var applicantLevel = applicant.JobTitle.Level;

        // 取得同部門中所有比申請人高的不重複 Level，由近到遠排列（DESC，因為 Level 越小越高）
        var superiorLevels = await db.Users.AsNoTracking()
            .Where(u => u.DepartmentId == applicant.DepartmentId
                && !u.IsSuperAdmin
                && u.JobTitle != null
                && u.JobTitle.Level < applicantLevel)
            .Select(u => u.JobTitle!.Level)
            .Distinct()
            .OrderByDescending(l => l) // 最接近的排前面
            .ToListAsync();

        return rank < superiorLevels.Count ? superiorLevels[rank] : null;
    }

    /// <inheritdoc />
    public async Task<(int nextStep, bool allSkipped)>
        SkipUnreviewableStepsAsync(int? approvalItemId, Guid applicantId, int fromStepOrder,
            IReadOnlyList<DesignatedReviewerRequest>? designatedReviewers = null,
            Guid? lastApproverId = null)
    {
        if (approvalItemId is null)
            return (fromStepOrder, false);

        var steps = await db.ApprovalSteps
            .AsNoTracking()
            .Where(s => s.ApprovalItemId == approvalItemId)
            .OrderBy(s => s.StepOrder)
            .ToListAsync();

        if (steps.Count == 0)
            return (fromStepOrder, false);

        var applicant = await db.Users.AsNoTracking()
            .Include(u => u.JobTitle)
            .FirstOrDefaultAsync(u => u.Id == applicantId);
        if (applicant is null)
            return (fromStepOrder, false);

        // 直接迭代 StepOrder >= fromStepOrder 的步驟（處理稀疏 StepOrder）
        var remainingSteps = steps.Where(s => s.StepOrder >= fromStepOrder).ToList();

        foreach (var step in remainingSteps)
        {
            // ── 第一階段：判斷該步驟是否「找不到審核者」 ──
            bool hasReviewer;
            if (step.UseApplicantDesignated)
            {
                var firstReviewer = designatedReviewers?
                    .OrderBy(r => r.StepOrder)
                    .FirstOrDefault();
                hasReviewer = firstReviewer is not null && firstReviewer.ReviewerId != applicantId;
            }
            else if (step.UseDirectSupervisor)
            {
                int rank = steps.Count(s => s.UseDirectSupervisor && s.StepOrder < step.StepOrder);
                var targetLevel = await FindNthSuperiorLevelAsync(applicant, rank);
                hasReviewer = targetLevel.HasValue;
            }
            else
            {
                hasReviewer = true; // 非上層級／非指定模式皆視為有審核者（既有行為）
            }

            if (!hasReviewer)
                continue; // 找不到審核者 → 跳過

            // ── 第二階段：若有「上一步核准者」，檢查本 step 解析出的唯一審核者是否就是同一人 ──
            if (lastApproverId.HasValue)
            {
                int directSupervisorRank = steps.Count(s => s.UseDirectSupervisor && s.StepOrder < step.StepOrder);
                var uniqueReviewerId = await ResolveUniqueReviewerAsync(
                    step, applicant, designatedReviewers, directSupervisorRank);

                if (uniqueReviewerId.HasValue && uniqueReviewerId.Value == lastApproverId.Value)
                    continue; // 唯一審核者 = 剛核准者 → 跳過，連續往後檢查
            }

            return (step.StepOrder, false); // 停在這步
        }

        // 所有剩餘步驟都跳過
        var maxStepOrder = steps.Max(s => s.StepOrder);
        return (maxStepOrder + 1, true);
    }

    /// <summary>
    /// 解析該步驟的「唯一審核者」：若該步驟解析出的審核者池剛好只有 1 位（且非申請人），回傳該 user Id；
    /// 否則回傳 null（包含 0 人、多人、無法解析三種情境）。
    /// 用於相鄰步驟同人自動跳過判定。
    /// </summary>
    private async Task<Guid?> ResolveUniqueReviewerAsync(
        Models.Entities.ApprovalStep step,
        Models.Entities.User applicant,
        IReadOnlyList<DesignatedReviewerRequest>? designatedReviewers,
        int directSupervisorRank)
    {
        // ── UseApplicantDesignated：取 designatedReviewers 中 StepOrder 最小者（pending 邏輯由呼叫端提供的清單已隱含） ──
        if (step.UseApplicantDesignated)
        {
            var firstReviewer = designatedReviewers?
                .OrderBy(r => r.StepOrder)
                .FirstOrDefault();
            if (firstReviewer is null) return null;
            if (firstReviewer.ReviewerId == applicant.Id) return null; // 自審情境不視為唯一審核者
            return firstReviewer.ReviewerId;
        }

        // ── UseDirectSupervisor：找該 rank 對應 Level，再查同部門該 Level 是否僅 1 位 ──
        if (step.UseDirectSupervisor)
        {
            var targetLevel = await FindNthSuperiorLevelAsync(applicant, directSupervisorRank);
            if (!targetLevel.HasValue || applicant.DepartmentId is null) return null;

            var candidates = await db.Users.AsNoTracking()
                .Where(u => u.DepartmentId == applicant.DepartmentId
                    && !u.IsSuperAdmin
                    && u.Id != applicant.Id
                    && u.JobTitle != null
                    && u.JobTitle.Level == targetLevel.Value)
                .Select(u => u.Id)
                .Take(2)
                .ToListAsync();

            return candidates.Count == 1 ? candidates[0] : null;
        }

        // ── 固定部門+職稱（含 UseApplicantDepartment）：查該部門 + 職稱 + 非 superadmin + 非申請人，唯一才回傳 ──
        int? targetDepartmentId = step.UseApplicantDepartment
            ? applicant.DepartmentId
            : step.DepartmentId;
        if (targetDepartmentId is null) return null;

        var query = db.Users.AsNoTracking()
            .Where(u => u.DepartmentId == targetDepartmentId.Value
                && !u.IsSuperAdmin
                && u.Id != applicant.Id);

        if (step.JobTitleId.HasValue)
            query = query.Where(u => u.JobTitleId == step.JobTitleId.Value);

        var fixedCandidates = await query
            .Select(u => u.Id)
            .Take(2)
            .ToListAsync();

        return fixedCandidates.Count == 1 ? fixedCandidates[0] : null;
    }

    /// <summary>判斷申請人是否符合此步驟的審核者條件（即「自己審自己」）</summary>
    private static bool IsApplicantTheReviewer(Models.Entities.ApprovalStep step, Models.Entities.User applicant)
    {
        bool jobTitleMatch = step.JobTitleId is null || step.JobTitleId == applicant.JobTitleId;

        bool deptMatch;
        if (step.UseApplicantDepartment)
        {
            deptMatch = true;
        }
        else
        {
            deptMatch = step.DepartmentId is null || step.DepartmentId == applicant.DepartmentId;
        }

        return jobTitleMatch && deptMatch;
    }
}
