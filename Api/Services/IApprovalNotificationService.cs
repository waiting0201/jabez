namespace Jabez.Api.Services;

/// <summary>簽核流程通知服務：在申請送出或審核動作後，寄發 Email 通知相關人員。</summary>
public interface IApprovalNotificationService
{
    /// <summary>通知下一步審核者有新的待審申請。</summary>
    Task NotifyReviewersAsync(
        string applicationType,
        int    applicationId,
        int?   approvalItemId,
        int    targetStepOrder,
        Guid   applicantId);

    /// <summary>請假送出後，通知被指定的職務代理人（僅記錄 + 通知，不參與簽核）。</summary>
    Task NotifyLeaveAgentAsync(int leaveRequestId);

    /// <summary>銷假核准後，通知原請假單的職務代理人代理已解除 / 部分解除。</summary>
    Task NotifyLeaveRevocationAgentAsync(int revocationId);

    /// <summary>通知申請人審核結果（核准/退回/拒絕）。</summary>
    /// <param name="contextLabel">附加在申請類型名稱後的說明（追加預支用，如「（第 2 次追加）」）。</param>
    Task NotifyApplicantAsync(
        string  applicationType,
        int     applicationId,
        Guid    applicantId,
        string  action,
        string? reviewNote,
        string? contextLabel = null);

    /// <summary>通知指定的升級審核者有新的待審申請。</summary>
    Task NotifySpecificReviewerAsync(
        string applicationType,
        int    applicationId,
        Guid   reviewerId,
        Guid   applicantId,
        bool   isDelegate);

    /// <summary>請款/預支申請核准後，通知財務部人員可以進行撥款。</summary>
    Task NotifyFinanceDeptAsync(int applicationId, Guid applicantId, string applicationType = "payment_request");

    /// <summary>沖銷結案時，若沖銷累計超過預支金額，通知財務部需匯款差額。</summary>
    Task NotifyFinanceRefundAsync(Models.Entities.AdvanceRequest advance, decimal refundAmount);

    /// <summary>出差沖銷結案時，若沖銷累計超過出差金額，通知財務部需匯款差額。</summary>
    Task NotifyFinanceTravelRefundAsync(Models.Entities.TravelRequest travel, decimal refundAmount);

    /// <summary>
    /// 財務確認撥款（PaidAt 從 null → 有值）後，通知申請人款項已撥付。
    /// 分期撥款情境下，每筆 installment 各自呼叫一次，標題會附「第 N/M 期」。
    /// </summary>
    /// <param name="installmentNo">當為分期撥款的單筆通知時的期數（null = 單筆 / 整單通知）</param>
    /// <param name="totalInstallments">總期數（與 installmentNo 配對使用）</param>
    Task NotifyApplicantPaidAsync(
        string   applicationType,
        int      applicationId,
        Guid     applicantId,
        decimal  amount,
        DateTime paidAt,
        int?     installmentNo      = null,
        int?     totalInstallments  = null);

    /// <summary>財務確認退款（RefundedAt 從 null → 有值）後，通知申請人退款已匯款。</summary>
    Task NotifyApplicantRefundedAsync(
        string   applicationType,
        int      applicationId,
        Guid     applicantId,
        decimal  refundAmount,
        DateTime refundedAt);

    /// <summary>
    /// 撥款日將屆提醒 — 對單一財務人員推一則彙整通知（LINE + Email）。
    /// 由 TimerTrigger 每日跑時呼叫。回傳 (emailSent, lineSent) 與失敗訊息（若有）。
    /// </summary>
    Task<(bool EmailSent, bool LineSent, string? ErrorMessage)> NotifyFinanceUpcomingPaymentsAsync(
        Guid financeUserId,
        IReadOnlyList<(string AppType, string AppLabel, int ApplicationId, string Applicant, DateTime ExpectedDate, decimal Amount)> items);
}
