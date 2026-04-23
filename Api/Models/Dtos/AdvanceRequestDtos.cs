namespace Jabez.Api.Models.Dtos;

// ── AdvanceRequestItem DTO ──────────────────────────────────────────────────

public sealed record AdvanceRequestItemDto(
    int      Id,
    string   Category,
    int      SeqNo,
    string   ItemName,
    decimal  UnitPrice,
    string   Quantity,
    decimal  TotalPrice,
    decimal  CashAmount,
    decimal  CheckAmount,
    string?  Note,
    int      SortOrder,
    string?  FileName = null,
    string?  FileUrl  = null);

public sealed record AdvanceRequestItemRequest(
    string   Category,
    int      SeqNo,
    string   ItemName,
    decimal  UnitPrice,
    string   Quantity,
    decimal  TotalPrice,
    decimal  CashAmount,
    decimal  CheckAmount,
    string?  Note,
    int      SortOrder);

// ── AdvanceRequest DTO ──────────────────────────────────────────────────────

public sealed record AdvanceRequestDto(
    int                       Id,
    string                    RequestNo,
    int                       ProjectId,
    string                    ProjectCode,
    string                    ProjectName,
    string                    ActivityName,
    string                    ActivityPeriod,
    DateTime                  AdvanceDate,
    decimal                   CashTotal,
    decimal                   CheckTotal,
    decimal                   GrandTotal,
    string                    ApprovalStatus,
    string?                   SubmittedBy,
    DateTime                  CreatedAt,
    DateTime?                 EstimatedPaymentDate,
    DateTime?                 PaidAt,
    DateTime?                 ReviewedAt,
    string?                   ReviewNote,
    AdvanceRequestItemDto[]   Items,
    WriteOffSummaryDto[]      WriteOffs,
    DesignatedReviewerDto[]?  DesignatedReviewers    = null,
    bool                      IsClosed               = false,
    DateTime?                 ClosedAt               = null,
    decimal?                  RefundAmount           = null,
    decimal?                  RefundedAmount         = null,
    DateTime?                 EstimatedRefundDate    = null,
    DateTime?                 RefundedAt             = null,
    WriteOffRecordDto[]?      WriteOffRecords        = null);

// ── WriteOff DTOs ───────────────────────────────────────────────────────────

public sealed record WriteOffItemDto(
    int       Id,
    string    Category,
    int       SeqNo,
    string    ItemName,
    decimal   UnitPrice,
    string    Quantity,
    decimal   TotalPrice,
    decimal   CashAmount,
    decimal   CheckAmount,
    string?   Note,
    string?   InvoiceNo,
    string?   FileName,
    string?   FileUrl,
    int       SortOrder,
    DateTime? InvoiceDate = null);

public sealed record WriteOffItemRequest(
    string    Category,
    int       SeqNo,
    string    ItemName,
    decimal   UnitPrice,
    string    Quantity,
    decimal   TotalPrice,
    decimal   CashAmount,
    decimal   CheckAmount,
    string?   Note,
    int       SortOrder,
    DateTime? InvoiceDate = null);

public sealed record WriteOffRecordDto(
    int               Id,
    string            RequestNo,
    int               WriteOffNo,
    decimal           CashTotal,
    decimal           CheckTotal,
    decimal           GrandTotal,
    string            ApprovalStatus,
    string?           Note,
    string?           SubmittedBy,
    DateTime          CreatedAt,
    WriteOffItemDto[] Items);

/// <summary>沖銷摘要（用於預支申請列表顯示）</summary>
public sealed record WriteOffSummaryDto(
    int      Id,
    int      WriteOffNo,
    decimal  GrandTotal,
    DateTime CreatedAt);

// ── Request DTOs ────────────────────────────────────────────────────────────

public sealed record CreateAdvanceRequestRequest(
    int                         ProjectId,
    string                      ActivityName,
    string                      ActivityPeriod,
    DateTime                    AdvanceDate,
    AdvanceRequestItemRequest[] Items,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

public sealed record UpdateAdvanceRequestRequest(
    int?                         ProjectId,
    string?                      ActivityName,
    string?                      ActivityPeriod,
    DateTime?                    AdvanceDate,
    AdvanceRequestItemRequest[]? Items,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

public sealed record CreateWriteOffRequest(
    WriteOffItemRequest[] Items,
    string?               Note);

// ── ApprovalTask 用 ─────────────────────────────────────────────────────────

public sealed record AdvanceTaskDetailDto(
    int       AdvanceRequestId,
    string    RequestNo,
    string    ProjectCode,
    string    ProjectName,
    string    ActivityName,
    decimal   GrandTotal,
    DateTime? EstimatedPaymentDate,
    DateTime? PaidAt,
    DateTime? EstimatedRefundDate,
    DateTime? RefundedAt,
    AdvanceRequestItemDto[] Items = null!,
    string?   PaidBySignatureUrl = null,
    decimal?  RefundAmount = null,
    decimal?  RefundedAmount = null)
{
    public AdvanceRequestItemDto[] Items { get; init; } = Items ?? Array.Empty<AdvanceRequestItemDto>();
}
