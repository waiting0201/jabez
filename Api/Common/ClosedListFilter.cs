namespace Jabez.Api.Common;

/// <summary>
/// 個人申請清單「進行中 / 已結案」頁籤（2026-11）的判定單一真相。
/// 各條件寫成**對裸表**的 SQL（清單分頁的 PagedIds CTE 與 COUNT 都直接 FROM 父表、無別名），
/// 以表名限定欄位，故可原樣放進兩處 WHERE。
///
/// 已結案的定義：
/// - 請款 / 出差請款（無結案概念）：已核准 且 各期撥款全部填了實際撥款日（＝ InstallmentReadService 的 FullyPaid）
/// - 預支 / 出差預支：財務已結案（IsClosed）。撥款完成不算 —— 之後還要沖銷，提早歸檔反而找不到
/// - 預支沖銷 / 出差預支沖銷：母單（預支 / 出差預支）已結案，同一張母單的沖銷整組一起歸檔
/// </summary>
public static class ClosedListFilter
{
    public const string PaymentRequest = """
        PaymentRequests.ApprovalStatus = 'approved'
        AND EXISTS (SELECT 1 FROM PaymentRequestInstallments ix WHERE ix.PaymentRequestId = PaymentRequests.Id)
        AND NOT EXISTS (SELECT 1 FROM PaymentRequestInstallments ix WHERE ix.PaymentRequestId = PaymentRequests.Id AND ix.PaidAt IS NULL)
        """;

    public const string TravelPaymentRequest = """
        TravelPaymentRequests.ApprovalStatus = 'approved'
        AND EXISTS (SELECT 1 FROM TravelPaymentRequestInstallments ix WHERE ix.TravelPaymentRequestId = TravelPaymentRequests.Id)
        AND NOT EXISTS (SELECT 1 FROM TravelPaymentRequestInstallments ix WHERE ix.TravelPaymentRequestId = TravelPaymentRequests.Id AND ix.PaidAt IS NULL)
        """;

    public const string AdvanceRequest = "AdvanceRequests.IsClosed = 1";

    public const string TravelRequest = "TravelRequests.IsClosed = 1";

    public const string WriteOffRecord =
        "EXISTS (SELECT 1 FROM AdvanceRequests pa WHERE pa.Id = WriteOffRecords.AdvanceRequestId AND pa.IsClosed = 1)";

    public const string TravelWriteOffRecord =
        "EXISTS (SELECT 1 FROM TravelRequests pt WHERE pt.Id = TravelWriteOffRecords.TravelRequestId AND pt.IsClosed = 1)";

    /// <summary>closed：true＝只要已結案、false＝只要進行中、null＝不篩（全部）</summary>
    public static string? Clause(string condition, bool? closed) => closed switch
    {
        true  => $"({condition})",
        false => $"NOT ({condition})",
        _     => null,
    };

    /// <summary>
    /// 由 query string 解析 <c>closed</c>：「true」→ true、「false」→ false，其餘（含未帶）→ null。
    /// 未帶時維持舊行為（全部），呼叫端（前端頁籤）一律明確帶值。
    /// </summary>
    public static bool? Parse(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "true"  => true,
        "false" => false,
        _       => null,
    };
}
