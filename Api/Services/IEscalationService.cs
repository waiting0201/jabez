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
    /// <param name="excludeUserIds">
    /// 需要排除的使用者集合。同人去重新規則限縮後，呼叫端應傳入「總監（JobTitle.Level=1）」歷史已審者集合
    /// （與 IApprovalFlowService.GetApprovedSupervisorIdsAsync 來源一致），避免 escalation 找回已經審過的總監；
    /// 升級鏈通常停在總監前所以實務影響小，但維持與 SkipUnreviewableStepsAsync 邏輯一致。
    /// null 表示不排除（例如初次送出申請時無歷史可比對）。
    /// </param>
    Task<EscalationResult?> TryEscalateAsync(
        Models.Entities.ApprovalStep step,
        Models.Entities.User applicant,
        string applicationType,
        IReadOnlySet<Guid>? excludeUserIds = null);

    /// <summary>
    /// 上層級步驟（UseDirectSupervisor）在申請人所屬部門找不到更高階審核者時，沿部門 ParentId 逐層往上尋找。
    /// 條件：active、非 superadmin、非申請人、有職稱且 JobTitle.Level &lt; 申請人 Level（＝確實比申請人高階）。
    /// 同一部門內有多位符合者時取「最接近申請人職級」的一位（Level 由大到小），避免直接跳到最頂層。
    ///
    /// 與 TryEscalateAsync 的差異：本方法處理的是「上層級關卡找不到人」而非「自審」，
    /// 故不看 step.UseApplicantDepartment / 不做自審判定，且**找不到時回傳 null 而非丟例外**
    /// （呼叫端維持既有的「跳過該關」行為，確保原本送得出的單不會因此送不出去）。
    /// 亦不套用請假／加班的「停在總監前」規則 —— 上層級關卡的語意就是往上找，
    /// 排除總監會讓部門最高主管仍然無人可審，等同此機制失效。
    /// </summary>
    /// <param name="applicant">申請人（需已 Include JobTitle）</param>
    /// <param name="excludeUserIds">需排除的使用者（同 TryEscalateAsync：傳入總監歷史已審者集合，避免找回已審過的人）</param>
    /// <returns>找到的上層審核者 Id；一路到頂層仍找不到則回 null</returns>
    Task<Guid?> FindSuperiorInAncestorDepartmentsAsync(
        Models.Entities.User applicant,
        IReadOnlySet<Guid>? excludeUserIds = null);
}
