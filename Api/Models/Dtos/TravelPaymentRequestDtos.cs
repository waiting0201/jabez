namespace Jabez.Api.Models.Dtos;

// ── 明細項目 DTO ────────────────────────────────────────────────────────────

public sealed record TravelPaymentRequestItemDto(
    int       Id,
    string    Category,
    int       SeqNo,
    string    ItemName,
    decimal   UnitPrice,
    string    Quantity,
    decimal   TotalPrice,
    string?   Note,
    int       SortOrder,
    string?   InvoiceNo   = null,
    string?   FileName    = null,
    string?   FileUrl     = null,
    DateTime? InvoiceDate = null);

public sealed record TravelPaymentRequestItemRequest(
    string    Category,
    int       SeqNo,
    string    ItemName,
    decimal   UnitPrice,
    string    Quantity,
    decimal   TotalPrice,
    string?   Note        = null,
    int       SortOrder   = 0,
    string?   InvoiceNo   = null,
    DateTime? InvoiceDate = null,
    string?   FileName    = null,
    string?   FileUrl     = null,
    int       FileIndex   = -1);

// ── 主申請單 DTO ────────────────────────────────────────────────────────────

public sealed record TravelPaymentRequestDto(
    int       Id,
    string    EmployeeName,
    string    Destination,
    DateTime  StartDate,
    DateTime  EndDate,
    decimal   GrandTotal,
    string    Purpose,
    int?      ProjectId,
    string?   ProjectCode,
    string?   ProjectName,
    string    ApprovalStatus,   // draft | pending | approved | rejected | returned
    DateTime  CreatedAt,
    DateTime? ReviewedAt,
    string?   ReviewNote,
    int?      ApprovalItemId           = null,
    int?      CurrentStepOrder         = null,
    Guid?     ReviewedById             = null,
    DateTime? EstimatedPaymentDate     = null,
    DateTime? PaidAt                   = null,
    TravelPaymentRequestItemDto[]?  Items               = null,
    DesignatedReviewerDto[]?        DesignatedReviewers = null);

// Note: Create/Update 接收 multipart/form-data，於 Handler 內直接從 form 讀取欄位，故不再需要 Create/Update Request DTO。
