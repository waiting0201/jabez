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
    DateTime?                 RefundedAt = null,
    decimal?                  AdvanceRefundAmount = null,
    decimal?                  AdvanceRefundedAmount = null,
    AttachmentDto[]?          Attachments = null);

// ── 可沖銷預支單 DTO ─────────────────────────────────────────────────────────

/// <summary>
/// 可沖銷的預支單（沖銷表單下拉選項 + 預支費用明細對照）。
/// Rounds / Items 含全部批次（含追加），供申請人對照填寫實際花費明細；
/// 追加簽核中的預支單不會出現在此清單（父單非 approved）。
/// </summary>
public sealed record AvailableAdvanceDto(
    int                     Id,
    string                  RequestNo,
    string                  ProjectCode,
    string                  ActivityName,
    DateTime                AdvanceDate,       // Round 1 預支日期
    decimal                 CashTotal,
    decimal                 CheckTotal,
    decimal                 GrandTotal,
    decimal                 WrittenOffTotal,
    AdvanceRoundDto[]       Rounds,            // 含 Round 1；Round ≥2 來自 AdvanceRequestSupplements
    AdvanceRequestItemDto[] Items);            // 全批次明細，已依 RoundNo, SortOrder, Id 排序

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
    string?            RefundedBySignatureUrl = null,
    bool               AdvanceIsClosed = false,
    decimal?           AdvanceRefundAmount = null,
    decimal?           AdvanceRefundedAmount = null,
    AttachmentDto[]?   Attachments = null);
