namespace Jabez.Api.Common;

/// <summary>
/// 發票號碼判定工具。
/// </summary>
public static class InvoiceNoHelper
{
    /// <summary>
    /// 發票號碼是否為手打文字（含中文 / CJK）。
    /// 含 CJK 統一表意文字者（如「收據」「領據」）視為手打文字，排除於重複檢查之外；
    /// 真正的統一發票為純英數（如 AB12345678），仍維持重複檢查。
    /// </summary>
    public static bool IsManualText(string? invoiceNo)
    {
        if (string.IsNullOrWhiteSpace(invoiceNo)) return false;
        foreach (var c in invoiceNo)
            if (c >= '一' && c <= '鿿') return true; // CJK 統一表意文字
        return false;
    }
}
