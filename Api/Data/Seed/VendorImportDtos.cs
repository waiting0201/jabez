namespace Jabez.Api.Data.Seed;

/// <summary>
/// 一次性廠商匯入中間 JSON（Api/Data/Seed/vendor-import.json）對應的 record。
/// 來源：reference/壯圍沙丘廠商匯款資料0812.xlsx（31 筆）。
/// 來源的「匯款帳號」一格內含四項（戶名／匯款銀行／銀行代號／銀行帳號），產 JSON 時已拆成四個欄位。
///
/// 刻意未納入的來源欄位：
///   - 序號：僅為 Excel 流水號，無業務意義
///
/// 來源沒有、因此匯入後為 NULL 的系統欄位：
///   - TaxId / IdNumber：來源未提供；Vendor 允許 NULL，但後台編輯時會被
///     VendorHandler.ValidateIdentifier 擋下，須先補件（見 Note 的待補標記）
///   - Address / BankBookImageUrl / IdCardFrontUrl / IdCardBackUrl：來源未提供
/// </summary>
public sealed class VendorImportRecord
{
    public string  Name            { get; set; } = "";
    public string? ContactPerson   { get; set; }
    public string? Phone           { get; set; }          // 多支號碼以 " / " 併接
    public string? BankAccountName { get; set; }          // 戶名（常與 Name 不同）
    public string? BankName        { get; set; }          // 匯款銀行（含分行）
    public string? BankCode        { get; set; }          // 銀行代號（農漁會為 xxx-xxxx，原樣保留）
    public string? BankAccount     { get; set; }          // 銀行帳號
    public string? Note            { get; set; }          // 別名 + 待補統編／存摺封面標記
}
