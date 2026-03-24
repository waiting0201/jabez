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

    /// <summary>通知申請人審核結果（核准/退回/拒絕）。</summary>
    Task NotifyApplicantAsync(
        string  applicationType,
        int     applicationId,
        Guid    applicantId,
        string  action,
        string? reviewNote);

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
}
