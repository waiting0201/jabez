namespace Jabez.Api.Models.Dtos;

// ── 指定審核者共用 DTOs ───────────────────────────────────────────────────────

/// <summary>指定審核者回應 DTO</summary>
public sealed record DesignatedReviewerDto(
    int       Id,
    Guid      ReviewerId,
    string    ReviewerName,
    int       StepOrder,
    string    Status,        // "pending" | "approved" | "returned"
    DateTime? ReviewedAt,
    string?   Comment);

/// <summary>指定審核者請求 DTO（用於 Create/Update/Submit）</summary>
public sealed record DesignatedReviewerRequest(
    Guid ReviewerId,
    int  StepOrder);

// ── Invoice DTOs ──────────────────────────────────────────────────────────────

public sealed record InvoiceItemDto(
    int       Id,
    string    FileName,
    string    InvoiceNo,
    decimal   Amount,
    string?   ItemName,
    string?   Note,
    string?   FileUrl,
    DateTime? InvoiceDate = null);

public sealed record InvoiceItemRequest(
    string    FileName,
    string    InvoiceNo,
    decimal   Amount,
    string?   ItemName,
    string?   Note,
    DateTime? InvoiceDate = null);

public sealed record PaymentRequestDto(
    int              Id,
    string           Type,
    int              ProjectId,
    string           ProjectCode,
    string           ProjectName,
    InvoiceItemDto[] Invoices,
    decimal          TotalAmount,
    string           ApprovalStatus,
    string?          SubmittedBy,
    DateTime         CreatedAt,
    DateTime?        EstimatedPaymentDate,
    DateTime?        PaidAt,
    DateTime?        ReviewedAt,
    string?          ReviewNote,
    string?          Reason = null,
    DesignatedReviewerDto[]? DesignatedReviewers = null);

public sealed record CreatePaymentRequestRequest(
    string              Type,
    int                 ProjectId,
    InvoiceItemRequest[] Invoices,
    string?             Reason = null,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

public sealed record UpdatePaymentRequestRequest(
    string?              Type,
    int?                 ProjectId,
    InvoiceItemRequest[] Invoices,
    string?             Reason = null,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

// 更新撥款日與狀態（財務部或 Superadmin 專用，paidAt 未填入前皆可修改）
public sealed record UpdatePaymentDateRequest(
    DateTime? EstimatedPaymentDate,
    DateTime? PaidAt,
    string?   ApprovalStatus,
    DateTime? EstimatedRefundDate = null,
    DateTime? RefundedAt = null,
    decimal?  RefundedAmount = null);

// 退還差額匯款日期
public sealed record RefundDateRequest(DateTime? RefundedAt);

// 審核動作（核准 / 退回修改 / 拒絕）
public sealed record ReviewPaymentRequestRequest(
    string    Action,           // "approved" | "returned" | "rejected"
    string?   ReviewNote,
    string    ApplicationType,  // "payment_request" | "leave" | "travel" | "overtime" | "advance" | "write_off"
    DateTime? EstimatedPaymentDate,  // 預計撥款日（僅請款/預支申請使用）
    DateTime? PaidAt,                // 撥款日（僅請款/預支申請使用）
    bool?     CloseAdvance);         // 預支結案（僅沖銷申請的財務部步驟使用）

public sealed record ApprovalRecordDto(
    int      StepOrder,
    string   Action,          // "approved" | "returned" | "rejected"
    string   ReviewedBy,
    DateTime ReviewedAt,
    string?  ReviewNote,
    string?  OnBehalfOf,      // 代理審核：代替誰審核（null 表示非代理）
    bool     IsEscalated,     // 是否為升級審核
    string?  ReviewerSignatureUrl = null);  // 審核者簽名檔 URL

// ── ApprovalTask 多型 DTOs ─────────────────────────────────────────────────

public sealed record ApprovalFlowStepDto(
    int     StepOrder,
    string? DepartmentName,
    string? DepartmentCode,
    string? JobTitleName,
    bool    UseDirectSupervisor,
    bool    UseApplicantDesignated,
    string? Note);

public sealed record ApprovalFlowDto(
    int                  Id,
    string               Name,
    ApprovalFlowStepDto[] Steps);

public sealed record PaymentTaskDetailDto(
    int              PaymentRequestId,
    string           PaymentType,
    string           ProjectCode,
    string           ProjectName,
    InvoiceItemDto[] Invoices,
    decimal          TotalAmount,
    DateTime?        EstimatedPaymentDate,
    DateTime?        PaidAt,
    string?          Reason = null,
    string?          PaidBySignatureUrl = null);

public sealed record LeaveTaskDetailDto(
    int      LeaveRequestId,
    string   LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    decimal  Hours,
    string   Reason);

public sealed record TravelTaskDetailDto(
    int       TravelRequestId,
    string    Destination,
    DateTime  StartDate,
    DateTime  EndDate,
    decimal   GrandTotal,
    string    Purpose,
    string?   ProjectCode,
    string?   ProjectName,
    bool      IsHolidayTravel,
    DateTime? EstimatedPaymentDate,
    DateTime? PaidAt,
    DateTime? EstimatedRefundDate,
    DateTime? RefundedAt,
    TravelRequestItemDto[] Items = null!,
    string?   PaidBySignatureUrl = null)
{
    public TravelRequestItemDto[] Items { get; init; } = Items ?? Array.Empty<TravelRequestItemDto>();
}

public sealed record OvertimeTaskDetailDto(
    int      OvertimeRequestId,
    DateTime OvertimeDate,
    string?  ProjectIds,
    decimal  EstimatedHours,
    string   Reason);

public sealed record ApprovalTaskDto(
    int                         Id,
    string                      ApplicationType,   // "payment_request" | "leave" | "travel" | "overtime" | "advance" | "write_off" | "travel_write_off"
    string                      Title,
    string                      SubmittedBy,
    DateTime                    SubmittedAt,
    string                      Status,
    int                         CurrentStepOrder,
    DateTime?                   ReviewedAt,
    string?                     ReviewNote,
    ApprovalFlowDto?            Flow,
    PaymentTaskDetailDto?       PaymentDetail,
    LeaveTaskDetailDto?         LeaveDetail,
    TravelTaskDetailDto?        TravelDetail,
    OvertimeTaskDetailDto?      OvertimeDetail,
    AdvanceTaskDetailDto?       AdvanceDetail,
    WriteOffTaskDetailDto?      WriteOffDetail,
    TravelWriteOffTaskDetailDto? TravelWriteOffDetail,
    ApprovalRecordDto[]         ApprovalRecords,
    DesignatedReviewerDto[]?    DesignatedReviewers     = null,
    string?                     SubmittedBySignatureUrl = null);  // 申請人簽名檔 URL
