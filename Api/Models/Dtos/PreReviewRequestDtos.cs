namespace Jabez.Api.Models.Dtos;

// ── PreReviewItem DTOs ────────────────────────────────────────────────────────

public sealed record PreReviewItemDto(
    int       Id,
    string    FileName,
    string?   ItemCategory,
    decimal   Amount,
    string?   ItemName,
    string?   Description,
    string?   Note,
    string?   FileUrl,
    DateTime? ItemDate = null);

public sealed record PreReviewItemRequest(
    string    FileName,
    string?   ItemCategory,
    decimal   Amount,
    string?   ItemName,
    string?   Description,
    string?   Note,
    DateTime? ItemDate = null);

// ── PreReviewRequest DTOs ─────────────────────────────────────────────────────

public sealed record PreReviewRequestDto(
    int                  Id,
    string               RequestNo,
    string               Type,
    int                  ProjectId,
    string               ProjectCode,
    string               ProjectName,
    PreReviewItemDto[]   Items,
    decimal              TotalAmount,
    decimal              TaxAmount,
    string               ApprovalStatus,
    string?              SubmittedBy,
    DateTime             CreatedAt,
    DateTime?            ReviewedAt,
    string?              ReviewNote,
    string?              Reason = null,
    DesignatedReviewerDto[]? DesignatedReviewers = null,
    int?                 VendorId    = null,
    string?              VendorName  = null,
    string?              VendorTaxId = null,
    AttachmentDto[]?     Attachments = null);

public sealed record CreatePreReviewRequestRequest(
    string               Type,
    int                  ProjectId,
    PreReviewItemRequest[] Items,
    string?              Reason = null,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

public sealed record UpdatePreReviewRequestRequest(
    string?               Type,
    int?                  ProjectId,
    PreReviewItemRequest[] Items,
    string?              Reason = null,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

// ── ApprovalTask 詳情 DTO（簽核作業頁 preReviewDetail）─────────────────────────

public sealed record PreReviewTaskDetailDto(
    int                  PreReviewRequestId,
    string               RequestNo,
    string               PaymentType,
    string               ProjectCode,
    string               ProjectName,
    PreReviewItemDto[]   Items,
    decimal              TotalAmount,
    decimal              TaxAmount,
    string?              Reason = null,
    int?                 VendorId            = null,
    string?              VendorName          = null,
    string?              VendorTaxId         = null,
    string?              VendorContactPerson = null,
    string?              VendorPhone         = null,
    string?              VendorBankAccount   = null,
    string?              VendorAddress       = null,
    AttachmentDto[]?     Attachments         = null);
