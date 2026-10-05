namespace Jabez.Api.Common;

/// <summary>
/// 申請單「明細金額」合理性單一真相（2026-10 安全稽核新增，純函式無 I/O，比照 <see cref="RequestDateGuard"/>）。
///
/// 在 Create / Update 收下明細、進入業務驗證之前呼叫，違規回 400。涵蓋：
///   · 單價 / 總價 / 現金 / 支票不可為負（0 允許：各表單既有的 <c>min</c> 皆為 0，零元列如贈品、全額支票月結的現金列為正常用法）。
///   · 沖銷 / 預支類「總價 = 現金 + 支票」（前端三欄連動的後端補強；容忍 0.01 浮點誤差）。
///   · 整單合計不可為負。
///
/// ⚠ 請款（payment_request）與預審（pre_review）**明細允許負數列**（2026-09 業務決議：折讓 / 退款 / 扣款以負數列表達，
/// 見 docs/business/application-forms.md），故這兩類以 <see cref="EnsureTotalNotNegative"/> 只擋「合計為負」，
/// 不擋單列為負。若業務改為連單列也禁止負數，把呼叫端換成 <see cref="EnsureItems"/> 即可。
/// </summary>
public static class AmountGuard
{
    /// <summary>浮點誤差容忍（與分期撥款 SUM 比對一致）。</summary>
    public const decimal Tolerance = 0.01m;

    /// <summary>單列金額欄位；不適用的欄位傳 null（不檢查）。</summary>
    public readonly record struct ItemAmounts(decimal? UnitPrice, decimal TotalPrice, decimal? Cash = null, decimal? Check = null);

    /// <summary>
    /// 檢查明細列：各金額欄 ≥ 0；<paramref name="requireCashPlusCheck"/> 時另要求 總價 = 現金 + 支票；合計 ≥ 0。
    /// </summary>
    /// <param name="kind">訊息用的明細名稱，例：「預支費用明細」。</param>
    public static void EnsureItems<T>(
        IEnumerable<T> rows, Func<T, ItemAmounts> selector, string kind, bool requireCashPlusCheck = false)
    {
        decimal total = 0m;
        int n = 0;
        foreach (var row in rows)
        {
            n++;
            var a = selector(row);
            string at = $"{kind}第 {n} 列";

            if (a.UnitPrice is < 0m) throw AppException.BadRequest($"{at}的單價不可為負數。");
            if (a.TotalPrice < 0m)   throw AppException.BadRequest($"{at}的金額不可為負數。");
            if (a.Cash is < 0m)      throw AppException.BadRequest($"{at}的現金金額不可為負數。");
            if (a.Check is < 0m)     throw AppException.BadRequest($"{at}的支票金額不可為負數。");

            if (requireCashPlusCheck
                && Math.Abs(a.TotalPrice - ((a.Cash ?? 0m) + (a.Check ?? 0m))) > Tolerance)
                throw AppException.BadRequest($"{at}的總價必須等於現金加支票金額。");

            total += a.TotalPrice;
        }

        EnsureTotalNotNegative(total, kind);
    }

    /// <summary>整單合計不可為負（請款 / 預審允許單列負數，只擋合計為負）。</summary>
    public static void EnsureTotalNotNegative(decimal total, string kind)
    {
        if (total < 0m)
            throw AppException.BadRequest($"{kind}的合計金額不可為負數。");
    }
}
