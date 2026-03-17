namespace Jabez.Api.Models.Dtos;

public sealed record InvoiceItemDto(
    int     Id,
    string  FileName,
    string  InvoiceNo,
    decimal Amount,
    string? ItemName,
    string? Note,
    string? FileUrl);

public sealed record InvoiceItemRequest(
    string  FileName,
    string  InvoiceNo,
    decimal Amount,
    string? ItemName,
    string? Note);

public sealed record PaymentRequestDto(
    int              Id,
    string           Type,
    int              ProjectId,
    string           ProjectCode,
    InvoiceItemDto[] Invoices,
    decimal          TotalAmount,
    string           ApprovalStatus,
    string?          SubmittedBy,
    DateTime         CreatedAt,
    DateTime?        EstimatedPaymentDate,
    DateTime?        PaidAt,
    DateTime?        ReviewedAt,
    string?          ReviewNote);

public sealed record CreatePaymentRequestRequest(
    string              Type,
    int                 ProjectId,
    InvoiceItemRequest[] Invoices);

public sealed record UpdatePaymentRequestRequest(
    string?              Type,
    int?                 ProjectId,
    InvoiceItemRequest[] Invoices);

// 更新撥款日（財務部或 Superadmin 專用）
public sealed record UpdatePaymentDateRequest(
    DateTime? EstimatedPaymentDate,
    DateTime? PaidAt);

// 審核動作（核准 / 退回修改 / 拒絕）
public sealed record ReviewPaymentRequestRequest(
    string    Action,           // "approved" | "returned" | "rejected"
    string?   ReviewNote,
    string    ApplicationType,  // "payment_request" | "leave" | "travel" | "overtime"
    DateTime? EstimatedPaymentDate,  // 預計撥款日（僅請款申請使用）
    DateTime? PaidAt);               // 撥款日（僅請款申請使用）

public sealed record ApprovalRecordDto(
    int      StepOrder,
    string   Action,          // "approved" | "returned" | "rejected"
    string   ReviewedBy,
    DateTime ReviewedAt,
    string?  ReviewNote,
    string?  OnBehalfOf,      // 代理審核：代替誰審核（null 表示非代理）
    bool     IsEscalated);    // 是否為升級審核

// ── ApprovalTask 多型 DTOs ─────────────────────────────────────────────────

public sealed record ApprovalFlowStepDto(
    int     StepOrder,
    string? DepartmentName,
    string? JobTitleName,
    string? Note);

public sealed record ApprovalFlowDto(
    int                  Id,
    string               Name,
    ApprovalFlowStepDto[] Steps);

public sealed record PaymentTaskDetailDto(
    int              PaymentRequestId,
    string           PaymentType,
    string           ProjectCode,
    InvoiceItemDto[] Invoices,
    decimal          TotalAmount,
    DateTime?        EstimatedPaymentDate,
    DateTime?        PaidAt);

public sealed record LeaveTaskDetailDto(
    int      LeaveRequestId,
    string   LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    decimal  Hours,
    string   Reason);

public sealed record TravelTaskDetailDto(
    int      TravelRequestId,
    string   Destination,
    DateTime StartDate,
    DateTime EndDate,
    decimal  EstimatedCost,
    string   Purpose,
    string?  ProjectCode,
    bool     IsHolidayTravel);

public sealed record OvertimeTaskDetailDto(
    int      OvertimeRequestId,
    DateTime OvertimeDate,
    string?  ProjectIds,
    decimal  EstimatedHours,
    string   Reason);

public sealed record ApprovalTaskDto(
    int                    Id,
    string                 ApplicationType,   // "payment_request" | "leave" | "travel" | "overtime" | "advance"
    string                 Title,
    string                 SubmittedBy,
    DateTime               SubmittedAt,
    string                 Status,
    int                    CurrentStepOrder,
    DateTime?              ReviewedAt,
    string?                ReviewNote,
    ApprovalFlowDto?       Flow,
    PaymentTaskDetailDto?  PaymentDetail,
    LeaveTaskDetailDto?    LeaveDetail,
    TravelTaskDetailDto?   TravelDetail,
    OvertimeTaskDetailDto? OvertimeDetail,
    AdvanceTaskDetailDto?  AdvanceDetail,
    ApprovalRecordDto[]    ApprovalRecords);
