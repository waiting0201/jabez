namespace Jabez.Api.Data.Seed;

/// <summary>
/// 一次性廠商匯入中間 JSON 對應的 record。資料檔名由 VENDOR_IMPORT_FILE 指定（預設 vendor-import.json）。
///
/// 已知來源檔：
///   - vendor-import.json（31 筆）：reference/壯圍沙丘廠商匯款資料0812.xlsx。
///     來源的「匯款帳號」一格內含四項（戶名／匯款銀行／銀行代號／銀行帳號），產 JSON 時已拆成四個欄位；
///     來源缺統編／身分證字號與地址，故該批的 TaxId / IdNumber / Address 皆為 NULL。
///   - vendor-import-1150820.json（109 筆＝廠商 79 ＋ 個人 30）：
///     reference/廠商及個人資料建置表_1150820.xlsx。兩個 sheet 欄位結構相同，唯一差別是
///     「廠商」sheet 的識別碼欄為統一編號（→ TaxId）、「個人」sheet 為身分證／居留證號（→ IdNumber）。
///
/// 刻意未納入的來源欄位：
///   - 編號 / 序號：僅為 Excel 流水號，無業務意義
///
/// 來源沒有、因此匯入後為 NULL 的系統欄位：
///   - BankBookImageUrl / IdCardFrontUrl / IdCardBackUrl：兩批來源皆未提供檔案；
///     存摺封面為 VendorHandler 的必填項，故匯入的廠商在後台編輯儲存時須先補件
/// </summary>
public sealed class VendorImportRecord
{
    public string  Name            { get; set; } = "";
    public string? TaxId           { get; set; }          // 統一編號（8 碼數字；與 IdNumber 擇一）
    public string? IdNumber        { get; set; }          // 身分證／居留證號碼（個人受款人）
    public string? ContactPerson   { get; set; }
    public string? Phone           { get; set; }          // 多支號碼以 " / " 併接
    public string? BankAccountName { get; set; }          // 戶名（常與 Name 不同）
    public string? BankName        { get; set; }          // 匯款銀行（含分行）
    public string? BankCode        { get; set; }          // 銀行代號（農漁會為 xxx-xxxx，原樣保留）
    public string? BankAccount     { get; set; }          // 銀行帳號
    public string? Address         { get; set; }          // 地址（個人來源為戶籍地址／通訊地址）
    public string? Note            { get; set; }          // 來源備註原文
}
