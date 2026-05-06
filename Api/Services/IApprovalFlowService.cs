using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services;

/// <summary>
/// 簽核流程輔助服務：處理送出申請時的步驟解析（如自動跳過自審步驟或升級審核）。
/// </summary>
public interface IApprovalFlowService
{
    /// <summary>
    /// 解析申請人送出後應從第幾步開始審核。
    /// 若申請人本身符合某步驟的審核者條件（自審），根據申請類型進行升級處理。
    /// 若所有步驟都被跳過，回傳 autoApproved = true。
    /// </summary>
    /// <param name="approvalItemId">簽核流程 ID</param>
    /// <param name="applicantId">申請人 User ID</param>
    /// <param name="applicationType">申請類型：overtime | leave | travel | payment_request | advance</param>
    /// <param name="designatedReviewers">申請人指定的審核者清單（UseApplicantDesignated 步驟使用）</param>
    /// <returns>
    /// startStep: 應開始的步驟序號
    /// autoApproved: 是否全部步驟都被跳過而自動核准
    /// escalation: 升級審核結果（null 表示無升級）
    /// </returns>
    Task<(int startStep, bool autoApproved, EscalationResult? escalation)>
        ResolveStartingStepAsync(int? approvalItemId, Guid applicantId, string applicationType,
            IReadOnlyList<DesignatedReviewerRequest>? designatedReviewers = null);

    /// <summary>
    /// 從指定步驟開始，跳過所有找不到審核者或「審核者池已被歷史已審者完全覆蓋」的步驟。
    /// 跨步驟同人去重（全歷史）：若某 step 的解析審核者池在扣除 approvedReviewerIds 後為空，
    /// 該 step 自動跳過，並由呼叫端對該 step 寫一筆代簽 ApprovalRecord（代簽人取池中最早審過此申請者）。
    /// 回傳：下一個有效步驟、是否全部跳過、被跳過的步驟清單（含代簽人 + 是否為 UseApplicantDesignated）。
    /// </summary>
    Task<(int nextStep, bool allSkipped, IReadOnlyList<SkippedStepInfo> skippedSteps)>
        SkipUnreviewableStepsAsync(int? approvalItemId, Guid applicantId, int fromStepOrder,
            IReadOnlyList<DesignatedReviewerRequest>? designatedReviewers = null,
            IReadOnlySet<Guid>? approvedReviewerIds = null,
            string? applicationType = null,
            int? applicationId = null);

    /// <summary>
    /// 取得此申請「最近一次 returned 之後」所有 approved 的審核者 Id（去重 HashSet）。
    /// 退回重送 → 歷史清零：以最近一次 Action='returned' 的 ReviewedAt 當分隔線。
    /// 從未被退回 → 等同全歷史。
    /// </summary>
    Task<HashSet<Guid>> GetApprovedReviewerIdsAsync(string applicationType, int applicationId);
}

/// <summary>
/// 描述被自動跳過的步驟（用於 ProcessReviewAsync 寫代簽 ApprovalRecord）。
/// </summary>
/// <param name="StepOrder">被跳過的步驟序號</param>
/// <param name="ProxyApproverId">代簽人（池 ∩ 歷史已審者，按 ReviewedAt 升序取首位）</param>
/// <param name="IsApplicantDesignated">該 step 是否為 UseApplicantDesignated（需同步更新 RequestDesignatedReviewers）</param>
public record SkippedStepInfo(int StepOrder, Guid ProxyApproverId, bool IsApplicantDesignated);
