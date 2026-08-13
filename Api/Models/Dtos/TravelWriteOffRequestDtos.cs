namespace Jabez.Api.Models.Dtos;

// ── 出差沖銷申請 DTO ──────────────────────────────────────────────────────────────

public sealed record TravelWriteOffRequestDto(
    int                           Id,
    string                        RequestNo,
    int                           TravelRequestId,
    string                        TravelRequestNo,
    int                           WriteOffNo,
    string                        Destination,
    DateTime                      StartDate,
    DateTime                      EndDate,
    string                        Purpose,
    string                        ProjectCode,
    string                        ProjectName,
    decimal                       GrandTotal,
    string?                       Note,
    string                        ApprovalStatus,
    string?                       SubmittedBy,
    DateTime                      CreatedAt,
    DateTime?                     ReviewedAt,
    string?                       ReviewNote,
    TravelWriteOffItemDto[]       Items,
    DesignatedReviewerDto[]?      DesignatedReviewers   = null,
    decimal                       TravelGrandTotal      = 0,
    decimal                       TravelWrittenOffTotal = 0,
    bool                          TravelIsClosed        = false,
    DateTime?                     EstimatedRefundDate   = null,
    DateTime?                     RefundedAt            = null,
    decimal?                      TravelRefundAmount    = null,
    decimal?                      TravelRefundedAmount  = null,
    DateTime?                     TravelClosedAt        = null);  // 關聯出差單的結案時間（供沖銷頁「出差單結案資訊」卡）

// ── Item DTO ──────────────────────────────────────────────────────────────────

public sealed record TravelWriteOffItemDto(
    int       Id,
    string    Category,
    int       SeqNo,
    string    ItemName,
    decimal   UnitPrice,
    string    Quantity,
    decimal   TotalPrice,
    string?   Note,
    string?   InvoiceNo,
    string?   FileName,
    string?   FileUrl,
    int       SortOrder,
    DateTime? InvoiceDate = null);

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed record CreateTravelWriteOffRequestRequest(
    int                              TravelRequestId,
    TravelWriteOffItemRequest[]      Items,
    string?                          Note,
    DesignatedReviewerRequest[]?     DesignatedReviewers = null);

public sealed record UpdateTravelWriteOffRequestRequest(
    TravelWriteOffItemRequest[]?     Items,
    string?                          Note,
    DesignatedReviewerRequest[]?     DesignatedReviewers = null);

public sealed record TravelWriteOffItemRequest(
    string    Category,
    int       SeqNo,
    string    ItemName,
    decimal   UnitPrice,
    string    Quantity,
    decimal   TotalPrice,
    string?   Note,
    int       SortOrder,
    DateTime? InvoiceDate = null);

// ── ApprovalTask 用 ──────────────────────────────────────────────────────────

public sealed record TravelWriteOffTaskDetailDto(
    int                      TravelWriteOffRequestId,
    int                      TravelRequestId,
    string                   RequestNo,
    string                   TravelRequestNo,
    string                   Destination,
    DateTime                 StartDate,
    DateTime                 EndDate,
    string                   Purpose,
    string                   ProjectCode,
    string                   ProjectName,
    decimal                  GrandTotal,
    string?                  Note,
    TravelWriteOffItemDto[]  Items,
    DateTime?                EstimatedRefundDate   = null,
    DateTime?                RefundedAt            = null,
    decimal                  TravelGrandTotal      = 0,
    decimal                  OtherWrittenOffTotal  = 0,
    string?                  RefundedBySignatureUrl = null,
    bool                     TravelIsClosed        = false,
    decimal?                 TravelRefundAmount    = null,
    decimal?                 TravelRefundedAmount  = null,
    DateTime?                TravelClosedAt        = null,
    bool                     PendingClose          = false);  // 財務已登記結案，待整張沖銷單核准後生效
