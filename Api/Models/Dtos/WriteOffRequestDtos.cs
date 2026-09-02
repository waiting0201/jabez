namespace Jabez.Api.Models.Dtos;

// ── 預支沖銷申請 DTO ──────────────────────────────────────────────────────────────

public sealed record WriteOffRequestDto(
    int                       Id,
    string?                   RequestNo,
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
    DateTime?                 SubmittedAt,  // 送簽日期（申請日期）；草稿為 null
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
    AttachmentDto[]?          Attachments = null,
    // ── 批次金額檢視 ──
    AdvanceRoundDto[]?        AdvanceRounds = null,       // 關聯預支單的各預支批次（含追加）
    WriteOffRoundDto[]?       WriteOffHistory = null,     // 同一預支單底下各張沖銷單
    decimal                   RefundDue = 0,              // 本次沖銷造成的超支增額（公司應補撥金額）
    // ── 分期撥款 ──
    InstallmentDto[]?         Installments = null,        // 本沖銷單的差額撥款分期
    string?                   PaymentStatus = null,
    InstallmentDto[]?         AdvanceInstallments = null, // 關聯預支單的撥款分期（唯讀對照）
    string?                   AdvancePaymentStatus = null,
    DateTime?                 AdvanceClosedAt = null);     // 關聯預支單的結案時間（供沖銷頁「預支單結案資訊」卡）

/// <summary>同一張預支單底下的各次沖銷（供沖銷資訊卡列出第 N 次沖銷金額）</summary>
public sealed record WriteOffRoundDto(
    int      Id,
    int      WriteOffNo,
    string?  RequestNo,
    decimal  GrandTotal,
    string   ApprovalStatus,
    DateTime CreatedAt,
    bool     IsCurrent);

// ── 依預支單彙總檢視 DTO ─────────────────────────────────────────────────────

/// <summary>
/// 依預支單彙總：一張預支單的完整資訊 + 該單底下全部沖銷單的完整資訊。
/// 供沖銷清單母層「檢視」開啟的彙總頁使用（GET /write-off-requests/by-advance/{advanceRequestId}）。
/// </summary>
public sealed record AdvanceWriteOffOverviewDto(
    AdvanceRequestDto    Advance,
    WriteOffRequestDto[] WriteOffs);

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
    decimal                 WrittenOffTotal,       // 只計已核准，與詳情頁 / 差額撥款同基準
    decimal                 PendingWriteOffTotal,  // 草稿 / 簽核中 / 已退回的沖銷金額（表單提示用，不計入餘額）
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
    AttachmentDto[]?   Attachments = null,
    // ── 批次金額檢視 ──
    AdvanceRoundDto[]?  AdvanceRounds = null,
    WriteOffRoundDto[]? WriteOffHistory = null,
    decimal             RefundDue = 0,
    // ── 分期撥款 ──
    InstallmentDto[]?   Installments = null,         // 本沖銷單的差額撥款分期（SUM 須等於 RefundDue）
    string?             PaymentStatus = null,
    InstallmentDto[]?   AdvanceInstallments = null,  // 關聯預支單的撥款分期（簽核頁可編輯，與預支單同步）
    string?             AdvancePaymentStatus = null,
    DateTime?           AdvanceClosedAt = null,       // 關聯預支單的結案時間（供簽核頁「預支單結案資訊」卡）
    bool                PendingClose = false);        // 財務已登記結案，待整張沖銷單核准後生效

// ── 支票已支付註記 ────────────────────────────────────────────────────────────

public sealed record UpdateCheckPaymentsRequest(
    CheckPaymentInput[] Items);

public sealed record CheckPaymentInput(
    int  ItemId,
    bool CheckPaid);
