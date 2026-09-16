using Jabez.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Common;

/// <summary>
/// 發票號碼唯一性檢查的**單一真相**：批次內去重 + 跨四張明細表全系統唯一。
///
/// 涵蓋的四張明細表（＝所有「實際報帳」型單據）：
///   InvoiceItems（請款）/ WriteOffItems（預支沖銷）/
///   TravelWriteOffItems（出差沖銷）/ TravelPaymentRequestItems（出差請款）
///
/// 【規則】
///   · 只排除 <c>rejected</c>：草稿（draft）與退回修改中（returned）的單**仍佔號** ——
///     單子還活著、之後會再送簽，提前釋放等於允許同一張發票被兩張單同時列入。
///   · 已拒絕的單一律不佔號，且**四張表皆然**（2026-09 修正：原本三個 Handler 各寫一份，
///     跨表查詢時漏加 <c>!= "rejected"</c>，被拒絕的沖銷單會永久佔住號碼、申請人無從自救）。
///   · 更新場景以 <c>self</c> 排除自身明細。
///   · 含中文 / CJK 的手打文字（「收據」「領據」）一律排除，見 <see cref="InvoiceNoHelper.IsManualText"/>。
///
/// 【錯誤訊息點名佔用者】撞號時必須講清楚是**哪張單**佔的（單別／單號／申請人／狀態），
/// 否則使用者面對「發票號碼已存在」完全無從自救 —— 尤其佔號者是自己或同事一張從未送簽的
/// 草稿時（例：一次掃多張發票試 OCR 而留下的測試單），使用者根本不會聯想到草稿也算數。
///
/// <see cref="Models.Entities.TravelRequestItem.InvoiceNo"/>（假日執行活動）**刻意不納入**：
/// 該欄只有 TravelRequestReadService 讀得出來，TravelRequestHandler 無任何寫入路徑，值恆 null。
/// 日後若假日活動開放填發票，在此加第 5 個 source 即可。
/// </summary>
public static class InvoiceUniquenessChecker
{
    /// <summary>發票明細的來源單別（供更新時排除自身）。</summary>
    public enum InvoiceSource
    {
        PaymentRequest,
        WriteOff,
        TravelWriteOff,
        TravelPayment,
    }

    private const string RejectedStatus = "rejected";

    /// <summary>單別中文名稱（錯誤訊息用）。</summary>
    private static readonly Dictionary<InvoiceSource, string> SourceLabels = new()
    {
        [InvoiceSource.PaymentRequest] = "請款單",
        [InvoiceSource.WriteOff]       = "預支沖銷單",
        [InvoiceSource.TravelWriteOff] = "出差沖銷單",
        [InvoiceSource.TravelPayment]  = "出差請款單",
    };

    /// <summary>簽核狀態中文（錯誤訊息用；rejected 已排除故不列）。</summary>
    private static readonly Dictionary<string, string> StatusLabels = new()
    {
        ["draft"]    = "草稿",
        ["pending"]  = "簽核中",
        ["returned"] = "退回修改中",
        ["approved"] = "已核准",
    };

    /// <summary>一筆佔號紀錄（查詢投影用）。</summary>
    private sealed record Occupancy(string InvoiceNo, InvoiceSource Source, string? RequestNo, string? ApplicantName, string Status);

    /// <summary>
    /// 驗證發票號碼唯一性；撞號時丟 <see cref="AppException"/>（409 Conflict）。
    /// </summary>
    /// <param name="db">DbContext</param>
    /// <param name="invoiceNos">本次送出的全部發票號碼（可含 null / 空字串 / 手打中文，內部自行過濾）</param>
    /// <param name="self">更新場景傳入自身單別與 ID，以排除自己的明細；新增場景傳 null</param>
    public static async Task EnsureUniqueAsync(
        AppDbContext db,
        IEnumerable<string?> invoiceNos,
        (InvoiceSource Source, int Id)? self = null)
    {
        // 手打中文文字（「收據」「領據」）排除於重複檢查之外
        var nos = invoiceNos
            .Where(n => !string.IsNullOrWhiteSpace(n) && !InvoiceNoHelper.IsManualText(n))
            .Select(n => n!)
            .ToList();

        if (nos.Count == 0) return;

        // ── 批次內重複檢查 ────────────────────────────────────────────────────
        var duplicatesInBatch = nos
            .GroupBy(n => n)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicatesInBatch.Count > 0)
            throw AppException.Conflict($"發票號碼重複：{string.Join("、", duplicatesInBatch)}");

        // ── 跨四張明細表唯一性檢查（一律排除已拒絕的單）────────────────────────
        var occupancies = new List<Occupancy>();

        var excludePaymentId       = self is { Source: InvoiceSource.PaymentRequest } s1 ? s1.Id : (int?)null;
        var excludeWriteOffId      = self is { Source: InvoiceSource.WriteOff }       s2 ? s2.Id : (int?)null;
        var excludeTravelWriteOffId = self is { Source: InvoiceSource.TravelWriteOff } s3 ? s3.Id : (int?)null;
        var excludeTravelPaymentId = self is { Source: InvoiceSource.TravelPayment }  s4 ? s4.Id : (int?)null;

        occupancies.AddRange(await db.InvoiceItems
            .AsNoTracking()
            .Where(ii => nos.Contains(ii.InvoiceNo)
                      && ii.PaymentRequest.ApprovalStatus != RejectedStatus
                      && (excludePaymentId == null || ii.PaymentRequestId != excludePaymentId))
            .Select(ii => new Occupancy(
                ii.InvoiceNo,
                InvoiceSource.PaymentRequest,
                ii.PaymentRequest.RequestNo,
                ii.PaymentRequest.SubmittedBy!.Name,
                ii.PaymentRequest.ApprovalStatus))
            .ToListAsync());

        occupancies.AddRange(await db.WriteOffItems
            .AsNoTracking()
            .Where(wi => nos.Contains(wi.InvoiceNo!)
                      && wi.WriteOffRecord.ApprovalStatus != RejectedStatus
                      && (excludeWriteOffId == null || wi.WriteOffRecordId != excludeWriteOffId))
            .Select(wi => new Occupancy(
                wi.InvoiceNo!,
                InvoiceSource.WriteOff,
                wi.WriteOffRecord.RequestNo,
                wi.WriteOffRecord.SubmittedBy!.Name,
                wi.WriteOffRecord.ApprovalStatus))
            .ToListAsync());

        occupancies.AddRange(await db.TravelWriteOffItems
            .AsNoTracking()
            .Where(twi => nos.Contains(twi.InvoiceNo!)
                       && twi.TravelWriteOffRecord.ApprovalStatus != RejectedStatus
                       && (excludeTravelWriteOffId == null || twi.TravelWriteOffRecordId != excludeTravelWriteOffId))
            .Select(twi => new Occupancy(
                twi.InvoiceNo!,
                InvoiceSource.TravelWriteOff,
                twi.TravelWriteOffRecord.RequestNo,
                twi.TravelWriteOffRecord.SubmittedBy!.Name,
                twi.TravelWriteOffRecord.ApprovalStatus))
            .ToListAsync());

        occupancies.AddRange(await db.TravelPaymentRequestItems
            .AsNoTracking()
            .Where(tpi => nos.Contains(tpi.InvoiceNo!)
                       && tpi.TravelPaymentRequest.ApprovalStatus != RejectedStatus
                       && (excludeTravelPaymentId == null || tpi.TravelPaymentRequestId != excludeTravelPaymentId))
            .Select(tpi => new Occupancy(
                tpi.InvoiceNo!,
                InvoiceSource.TravelPayment,
                tpi.TravelPaymentRequest.RequestNo,
                tpi.TravelPaymentRequest.Employee!.Name,
                tpi.TravelPaymentRequest.ApprovalStatus))
            .ToListAsync());

        if (occupancies.Count == 0) return;

        // 同一號碼可能被多筆明細命中（如同一張單列兩次），以號碼分組後各取一筆呈現
        var details = occupancies
            .GroupBy(o => o.InvoiceNo)
            .OrderBy(g => g.Key)
            .Select(g => Describe(g.First()))
            .ToList();

        throw AppException.Conflict($"發票號碼已被使用：{string.Join("；", details)}");
    }

    /// <summary>組成「AB12345678（預支沖銷單 WO-20260905-001／王小明／草稿）」。</summary>
    private static string Describe(Occupancy o)
    {
        var requestNo = string.IsNullOrWhiteSpace(o.RequestNo) ? "尚未取號" : o.RequestNo;
        var applicant = string.IsNullOrWhiteSpace(o.ApplicantName) ? "申請人不詳" : o.ApplicantName;
        var status    = StatusLabels.TryGetValue(o.Status, out var label) ? label : o.Status;
        return $"{o.InvoiceNo}（{SourceLabels[o.Source]} {requestNo}／{applicant}／{status}）";
    }
}
