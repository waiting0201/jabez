using System.Globalization;
using System.Text.RegularExpressions;

namespace Jabez.Api.Data.Seed;

/// <summary>
/// 一次性員工匯入專用：解析人事資料卡上的日期字串。
/// 同時支援民國年（如 96年10月01日、066年11月10日）與西元年（1979年04月17日、2026年2月1日），
/// 以及 yyyy.MM.dd / yyyy/MM/dd 等變體。
/// 規則：抽出年/月/日數字後，年 ≤ 150 視為民國（+1911），否則視為西元。
/// 缺月或缺日預設為 1；完全無月份數字則回傳 null（無法判定）。
/// </summary>
public static partial class RocDateParser
{
    // 擷取「年/月/日」三段數字（年必含，月日可缺）。容許全形空白與多餘空白。
    [GeneratedRegex(@"(\d{1,4})\s*年\s*(\d{1,2})?\s*月?\s*(\d{1,2})?\s*日?")]
    private static partial Regex YmdRegex();

    // 純分隔符格式：yyyy.MM.dd / yyyy/MM/dd / yyyy-MM-dd（年可為 2-4 碼，視為民國或西元）。
    [GeneratedRegex(@"^(\d{2,4})[./\-](\d{1,2})[./\-](\d{1,2})$")]
    private static partial Regex SepRegex();

    public static DateTime? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim();

        var sep = SepRegex().Match(s);
        if (sep.Success)
            return Build(int.Parse(sep.Groups[1].Value), int.Parse(sep.Groups[2].Value), int.Parse(sep.Groups[3].Value));

        var m = YmdRegex().Match(s);
        if (m.Success)
        {
            int year  = int.Parse(m.Groups[1].Value);
            int month = m.Groups[2].Success && m.Groups[2].Value.Length > 0 ? int.Parse(m.Groups[2].Value) : 1;
            int day   = m.Groups[3].Success && m.Groups[3].Value.Length > 0 ? int.Parse(m.Groups[3].Value) : 1;
            return Build(year, month, day);
        }

        // 最後退路：標準日期字串
        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? DateTime.SpecifyKind(d, DateTimeKind.Unspecified)
            : null;
    }

    private static DateTime? Build(int year, int month, int day)
    {
        if (year <= 0) return null;
        if (year <= 150) year += 1911;          // 民國 → 西元
        if (month is < 1 or > 12) month = 1;
        if (day   is < 1 or > 31) day   = 1;
        try
        {
            return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
