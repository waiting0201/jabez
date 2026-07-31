namespace Jabez.Api.Data.Seed;

/// <summary>
/// 一次性專案匯入中間 JSON（Api/Data/Seed/project-import.json）對應的 record。
/// 來源：reference/專案資料-115.07.29.xls。
/// 日期欄位皆為民國年原始字串（如 115.07.29），由 <see cref="RocDateParser"/> 解析。
///
/// 刻意未納入的來源欄位：
///   - 實收金額：系統為衍生值（SUM(ProjectPaymentSchedules.DepositAmount)），非可寫入欄位
///   - 業務執行金額 / 剩餘金額：來源 34 筆全為 0 或空白，一律匯 NULL（未設定）避免專案水位報表誤判
///   - 扣款金額：系統為畫面即時計算（發票 − 入帳），不存 DB
///   - Google雲端硬碟連結 / 請款期別明細：來源全空
/// </summary>
public sealed class ProjectImportRecord
{
    public string  Code             { get; set; } = "";
    public string  Name             { get; set; } = "";
    public string? StatusText       { get; set; }          // 進行中 / 已結案
    public string? DepartmentText   { get; set; }
    public string? StartDate        { get; set; }          // 缺值時套用佔位日，見 ProjectImporter.PlaceholderStartDate
    public string? EndDate          { get; set; }
    public decimal? ContractAmount  { get; set; }

    public List<ProjectScheduleImportRecord> PaymentSchedules { get; set; } = [];
}

/// <summary>專案請款期別明細（來源僅保留最新一期）</summary>
public sealed class ProjectScheduleImportRecord
{
    public string?   PeriodText    { get; set; }           // 第一期 / 第二期 …，解析成 PeriodNo
    public string?   BillingDate   { get; set; }
    public decimal?  BillingAmount { get; set; }
    public string?   InvoiceDate   { get; set; }
    public decimal?  InvoiceAmount { get; set; }
    public string?   DepositDate   { get; set; }
    public decimal?  DepositAmount { get; set; }
    public string?   DeductionNote { get; set; }
}
