using Jabez.Api.Data;
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
        ResolveStartingStepAsync(int? approvalItemId, Guid applicantId, string applicationType)
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

        var applicant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == applicantId);
        if (applicant is null)
            return (1, false, null);

        int currentStep = 1;
        foreach (var step in steps)
        {
            bool isSelfReview = IsApplicantTheReviewer(step, applicant);

            if (!isSelfReview)
            {
                // 請款：若步驟使用申請人部門但該部門無符合條件的審核者，也跳過
                if (applicationType == "payment_request" && step.UseApplicantDepartment && applicant.DepartmentId.HasValue)
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
