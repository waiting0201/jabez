using System.Text;
using System.Text.RegularExpressions;

namespace Jabez.Api.Common;

/// <summary>
/// 發票號碼判定與正規化工具（唯一性檢查的比對鍵單一真相）。
///
/// 2026-10 安全稽核：原本唯一性比對用「原始字串」，且含中文者整筆跳過，員工可用
/// 「ab12345678」「AB-12345678」「AB 12345678」「收據AB12345678」把同一張發票重複報帳。
/// 現改為比對前先正規化成比對鍵（<see cref="ResolveKey"/>）。
/// </summary>
public static partial class InvoiceNoHelper
{
    /// <summary>比對時一律剔除的空白類字元（含全形空白、不斷行空白）。</summary>
    public static readonly string[] RemovedWhitespace = [" ", "\t", "　", " "];

    /// <summary>比對時一律剔除的連字號類字元（半形 / 全形 / en dash / em dash / 連字符）。</summary>
    public static readonly string[] RemovedHyphens = ["-", "－", "–", "—", "‐"];

    /// <summary>
    /// 資料庫端前置篩選要剔除的字元全集（與 <see cref="Normalize"/> 完全一致，
    /// SQL 的 REPLACE 鏈與記憶體正規化才會對得起來）。
    /// </summary>
    public static IEnumerable<string> RemovedForSql => RemovedWhitespace.Concat(RemovedHyphens);

    /// <summary>
    /// 發票號碼是否為手打文字（含中文 / CJK）。
    /// 含 CJK 統一表意文字者（如「收據」「領據」）視為手打文字；
    /// 其中若仍可抽出標準統一發票號碼（<c>[A-Z]{2}\d{8}</c>），由 <see cref="ResolveKey"/> 以抽出的號碼比對，
    /// 抽不出才排除於重複檢查之外。
    /// </summary>
    public static bool IsManualText(string? invoiceNo)
    {
        if (string.IsNullOrWhiteSpace(invoiceNo)) return false;
        foreach (var c in invoiceNo)
            if (c >= '一' && c <= '鿿') return true; // CJK 統一表意文字
        return false;
    }

    /// <summary>
    /// 正規化：去前後空白、去中間空白與連字號、轉大寫。
    /// 純記憶體；「去哪些字元」見 <see cref="RemovedWhitespace"/> / <see cref="RemovedHyphens"/>。
    /// </summary>
    public static string Normalize(string? invoiceNo)
    {
        if (string.IsNullOrWhiteSpace(invoiceNo)) return string.Empty;

        var sb = new StringBuilder(invoiceNo.Length);
        foreach (var c in invoiceNo.Trim())
        {
            var s = c.ToString();
            if (RemovedWhitespace.Contains(s) || RemovedHyphens.Contains(s)) continue;
            sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }

    /// <summary>
    /// 唯一性比對鍵：
    ///   · 空值 → null（不檢查）。
    ///   · 正規化後不含中文 → 正規化字串本身。
    ///   · 正規化後含中文（手打文字，如「收據」）→ 仍抽得出 <c>[A-Z]{2}\d{8}</c> 者以抽出的發票號比對，
    ///     抽不出（純「收據」「領據」之類）→ null，排除於檢查之外。
    /// </summary>
    public static string? ResolveKey(string? invoiceNo)
    {
        var n = Normalize(invoiceNo);
        if (n.Length == 0) return null;
        if (!IsManualText(n)) return n;

        var m = InvoicePattern().Match(n);
        return m.Success ? m.Value : null;
    }

    /// <summary>標準統一發票號碼：兩碼英文 + 八碼數字；前後不可緊鄰英文 / 數字（避免從更長的字串中段誤抽）。</summary>
    [GeneratedRegex(@"(?<![A-Z])[A-Z]{2}\d{8}(?!\d)")]
    private static partial Regex InvoicePattern();
}
