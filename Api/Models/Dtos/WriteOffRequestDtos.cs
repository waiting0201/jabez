namespace Jabez.Api.Models.Dtos;

// ── 預支沖銷申請 DTO ──────────────────────────────────────────────────────────────

public sealed record WriteOffRequestDto(
    int                       Id,
    string                    RequestNo,
    int                       AdvanceRequestId,
    string                    AdvanceRequestNo,
    int                       WriteOffNo,
    string                    ProjectCode,
    string                    ProjectName,
    string                    ActivityName,
    string                    ActivityPeriod,
    decimal                   CashTotal,
    decimal                   CheckTotal,
    decimal                   GrandTotal,
    string?                   Note,
    string                    ApprovalStatus,
    string?                   SubmittedBy,
    DateTime                  CreatedAt,
    DateTime?                 ReviewedAt,
    string?                   ReviewNote,
    WriteOffItemDto[]         Items,
    DesignatedReviewerDto[]?  DesignatedReviewers = null,
    decimal                   AdvanceGrandTotal = 0,
    decimal                   AdvanceWrittenOffTotal = 0,
    bool                      AdvanceIsClosed = false,
    DateTime?                 EstimatedRefundDate = null,
    DateTime?                 RefundedAt = null);

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed record CreateWriteOffRequestRequest(
    int                          AdvanceRequestId,
    WriteOffItemRequest[]        Items,
    string?                      Note,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

public sealed record UpdateWriteOffRequestRequest(
    WriteOffItemRequest[]?       Items,
    string?                      Note,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

// ── ApprovalTask 用 ──────────────────────────────────────────────────────────

public sealed record WriteOffTaskDetailDto(
    int                WriteOffRequestId,
    int                AdvanceRequestId,
    string             RequestNo,
    string             AdvanceRequestNo,
    string             ProjectCode,
    string             ProjectName,
    decimal            GrandTotal,
    decimal            CashTotal,
    decimal            CheckTotal,
    string?            Note,
    WriteOffItemDto[]  Items,
    DateTime?          EstimatedRefundDate = null,
    DateTime?          RefundedAt = null,
    decimal            AdvanceGrandTotal = 0,
    decimal            OtherWrittenOffTotal = 0,
    string?            PaidBySignatureUrl = null,
    string?            RefundedBySignatureUrl = null,
    bool               AdvanceIsClosed = false);
