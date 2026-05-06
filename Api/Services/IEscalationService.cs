namespace Jabez.Api.Services;

/// <summary>
/// 簽核升級服務：當申請人即審核者（自審）時，根據申請類型往上層部門尋找合適的審核者。
/// </summary>
public interface IEscalationService
{
    /// <summary>
    /// 嘗試升級審核。若非自審情境，回傳 null。
    /// 若為自審但找不到合適審核者，拋出 AppException。
    /// </summary>
    /// <param name="step">當前步驟定義</param>
    /// <param name="applicant">申請人</param>
    /// <param name="applicationType">申請類型：overtime | leave | travel</param>
    /// <param name="excludeUserIds">需要排除的使用者集合（例如已在先前 step 審過此申請的人）；null 表示不排除。</param>
    Task<EscalationResult?> TryEscalateAsync(
        Models.Entities.ApprovalStep step,
        Models.Entities.User applicant,
        string applicationType,
        IReadOnlySet<Guid>? excludeUserIds = null);
}
