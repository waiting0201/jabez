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
    int      SortOrder);

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
    Guid?                     DesignatedReviewerId   = null,
    string?                   DesignatedReviewerName = null);

// ── WriteOff DTOs ───────────────────────────────────────────────────────────

public sealed record WriteOffItemDto(
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
    string?  InvoiceNo,
    string?  FileName,
    string?  FileUrl,
    int      SortOrder);

public sealed record WriteOffItemRequest(
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

public sealed record WriteOffRecordDto(
    int               Id,
    int               WriteOffNo,
    decimal           CashTotal,
    decimal           CheckTotal,
    decimal           GrandTotal,
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
    Guid?                       DesignatedReviewerId = null);

public sealed record UpdateAdvanceRequestRequest(
    int?                         ProjectId,
    string?                      ActivityName,
    string?                      ActivityPeriod,
    DateTime?                    AdvanceDate,
    AdvanceRequestItemRequest[]? Items,
    Guid?                        DesignatedReviewerId = null);

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
    DateTime? PaidAt);
