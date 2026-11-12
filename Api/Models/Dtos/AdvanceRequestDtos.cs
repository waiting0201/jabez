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
    string?  FileUrl  = null,
    int      RoundNo  = 1);

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

// ── 預支批次（追加預支）DTO ─────────────────────────────────────────────────

/// <summary>
/// 預支批次：RoundNo=1 為原始預支（日期取 AdvanceRequest.AdvanceDate），
/// RoundNo≥2 為第 N 次追加（取自 AdvanceRequestSupplements）。金額一律由該批次 Items 加總推導。
/// </summary>
public sealed record AdvanceRoundDto(
    int      RoundNo,
    DateTime AdvanceDate,
    string?  Reason,
    decimal  CashTotal,
    decimal  CheckTotal,
    decimal  GrandTotal,
    int      ItemCount,
    DateTime? AdvanceNeededDate = null);

// ── AdvanceRequest DTO ──────────────────────────────────────────────────────

public sealed record AdvanceRequestDto(
    int                       Id,
    string?                   RequestNo,
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
    DateTime?                 SubmittedAt,  // 送簽日期（申請日期）；草稿為 null
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
    WriteOffRecordDto[]?      WriteOffRecords        = null,
    InstallmentDto[]?         Installments           = null,
    string?                   PaymentStatus          = null,
    AdvanceRoundDto[]?        Rounds                 = null,
    int                       CurrentRoundNo         = 1,
    DateTime?                 AdvanceNeededDate      = null,
    // 公司已補撥的超支差額（已核准沖銷的已撥款差額分期合計 / 最後撥款日；詳情頁「尚未結清」為負時判定已結清用）
    decimal                   OverspendPaidTotal     = 0,
    DateTime?                 OverspendLastPaidAt    = null,
    IEnumerable<AdvanceLinkedPreReviewDto>? PreReviews = null,
    decimal?                  ActualAmount           = null,  // 結案寫回的實際金額（未結案 null）
    AdvanceSettlementLedgerDto? SettlementLedger     = null); // 金額歷程（僅單筆詳情帶入，列表為 null）

/// <summary>預支單關聯的預審單摘要（RoundNo = 第幾次預支加選的；1 = 原始預支）。</summary>
public sealed record AdvanceLinkedPreReviewDto(
    int       PreReviewRequestId,
    string?   RequestNo,
    int       RoundNo,
    string?   ProjectCode,
    string?   ProjectName,
    string?   Reason,
    decimal   TotalAmount,   // 未稅小計；含稅 = TotalAmount + TaxAmount
    decimal   TaxAmount,
    DateTime? SubmittedAt);

/// <summary>預支表單「可關聯預審單」清單項目：LinkedAdvanceNos = 引用它的【其他】預支單（單號或「草稿」，不含已拒絕）。</summary>
public sealed record LinkablePreReviewDto(
    int       Id,
    string?   RequestNo,
    string?   ProjectCode,
    string?   ProjectName,
    string?   Reason,
    decimal   TotalAmount,
    decimal   TaxAmount,
    DateTime? SubmittedAt,
    IEnumerable<string> LinkedAdvanceNos);

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
    DateTime? InvoiceDate   = null,
    bool      CheckPaid     = false,   // 支票已由公司付給廠商（財務勾選）
    DateTime? CheckPaidAt   = null,
    string?   CheckPaidBy   = null);

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
    string?           RequestNo,
    int               WriteOffNo,
    decimal           CashTotal,
    decimal           CheckTotal,
    decimal           GrandTotal,
    string            ApprovalStatus,
    string?           Note,
    string?           SubmittedBy,
    DateTime          CreatedAt,
    WriteOffItemDto[] Items,
    // ── 同仁繳回預支餘額（預支詳情頁「沖銷與繳回紀錄」卡 / 預支 PDF 用）──
    decimal?          ReturnedAmount        = null,
    DateTime?         ReturnedAt            = null,
    string?           ReturnedProofFileName = null,
    string?           ReturnedProofUrl      = null,
    bool              FinancePassed         = false);  // 財務管理部關卡已核准（判準同 WriteOffRequestReadService.FinancePassedSql）

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
    DesignatedReviewerRequest[]? DesignatedReviewers = null,
    DateTime?                   AdvanceNeededDate   = null);

public sealed record UpdateAdvanceRequestRequest(
    int?                         ProjectId,
    string?                      ActivityName,
    string?                      ActivityPeriod,
    DateTime?                    AdvanceDate,
    AdvanceRequestItemRequest[]? Items,
    DesignatedReviewerRequest[]? DesignatedReviewers = null,
    DateTime?                    AdvanceNeededDate   = null);

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
    string    ActivityPeriod,
    decimal   GrandTotal,
    DateTime? EstimatedRefundDate,
    DateTime? RefundedAt,
    AdvanceRequestItemDto[] Items = null!,
    decimal?  RefundAmount = null,
    decimal?  RefundedAmount = null,
    InstallmentDto[]? Installments = null,
    string?   PaymentStatus = null,
    AdvanceRoundDto[]? Rounds = null,
    int       CurrentRoundNo = 1,
    bool      IsClosed = false,
    DateTime? ClosedAt = null,
    DateTime? AdvanceNeededDate = null,
    IEnumerable<AdvanceLinkedPreReviewDto>? PreReviews = null,
    decimal?  ActualAmount = null,
    AdvanceSettlementLedgerDto? SettlementLedger = null)
{
    public AdvanceRequestItemDto[] Items { get; init; } = Items ?? Array.Empty<AdvanceRequestItemDto>();
}

// ── 預支金額歷程（Settlement Ledger，2026-11）──────────────────────────────

/// <summary>
/// 預支 / 出差預支「從預支到實際金額」的歷程單一真相（由 <c>AdvanceSettlementLedgerService</c> 產出）。
/// 預支、沖銷、出差各頁的「已沖銷 / 尚未結清 / 實際金額 / 差額」一律讀這份，前端不得各自重算。
/// 基準：只計已核准的沖銷；簽核中 / 退回 / 草稿的沖銷另列 <see cref="PendingWriteOffTotal"/>，不進累計。
/// **「尚未結清」走保守互抵規則（2026-11，<c>WriteOffRefundCalculator.Settle</c>）**：現金差額與總額差額同方向取較小者。
/// 月結（支票）由公司直接支付廠商，月結差額另列 <see cref="CheckDifference"/>；
/// 現金與月結互抵的部分（<see cref="CashDifference"/> ≠ <see cref="Outstanding"/>）亦不算應繳回 / 應補撥。
/// </summary>
public sealed record AdvanceSettlementLedgerDto(
    decimal   AdvanceTotal,           // 預支總額（含已核准的追加批次）
    decimal   ApprovedWriteOffTotal,  // 已核准沖銷合計
    decimal   ReturnedTotal,          // 已核准沖銷登錄的同仁繳回合計（出差預支恆 0）
    decimal   OverspendPaidTotal,     // 公司已補撥的超支差額（已核准沖銷的差額分期已撥合計；出差預支恆 0）
    decimal   Outstanding,            // 尚未結清 = Settle(CashDifference, 總額差額)：兩者同方向取較小者，否則 0（>0 同仁尚應繳回，<0 公司尚應補撥）
    decimal   PendingWriteOffTotal,   // 簽核中 / 退回 / 草稿的沖銷（不進累計）
    bool      IsClosed,
    DateTime? ClosedAt,
    string?   ClosedByName,
    decimal?  ActualAmount,           // 結案寫回的實際金額（未結案為 null；舊結案單由 migration 回填）
    decimal?  Difference,             // = AdvanceTotal − (ActualAmount ?? ApprovedWriteOffTotal)
    List<AdvanceSettlementEventDto> Events,
    // ── 月結（支票）分項（2026-11；出差預支恆 0）──
    decimal   AdvanceCheckTotal = 0,          // 月結預支（已核准批次）
    decimal   ApprovedWriteOffCheckTotal = 0, // 已核准沖銷的月結合計
    decimal   CheckDifference = 0,            // = AdvanceCheckTotal − ApprovedWriteOffCheckTotal（>0 月結實支較預支少，<0 較多）；公司與廠商之間，不計入 Outstanding
    decimal   CashDifference = 0);            // 現金側原始差額 = 現金預支 − 現金核銷 − ReturnedTotal + OverspendPaidTotal；≠ Outstanding 代表有一部分與月結互抵

/// <summary>
/// 歷程事件列。Kind：advance_round / advance_paid / write_off / returned / overspend_paid / closed。
/// Running* 為該列（含）之前的累計；advance_paid 為資訊列，不影響 RunningOutstanding。
/// RunningAdvance / RunningWrittenOff 為總額（含月結）；RunningOutstanding 同 Outstanding 的保守互抵規則，
/// 含月結的列於 Note 標「含月結 N 元」。
/// </summary>
public sealed record AdvanceSettlementEventDto(
    DateTime? Date,
    string    Kind,
    string    Label,
    string?   RequestNo,
    int?      WriteOffId,
    decimal   Amount,
    decimal   RunningAdvance,
    decimal   RunningWrittenOff,
    decimal   RunningOutstanding,
    string?   Note);
