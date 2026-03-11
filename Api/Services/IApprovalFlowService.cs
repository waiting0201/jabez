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
    /// <param name="applicationType">申請類型：overtime | leave | travel | payment_request</param>
    /// <returns>
    /// startStep: 應開始的步驟序號
    /// autoApproved: 是否全部步驟都被跳過而自動核准
    /// escalation: 升級審核結果（null 表示無升級）
    /// </returns>
    Task<(int startStep, bool autoApproved, EscalationResult? escalation)>
        ResolveStartingStepAsync(int? approvalItemId, Guid applicantId, string applicationType);
}
