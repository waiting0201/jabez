namespace Jabez.Api.Models.Entities;

public class TravelRequestItem
{
    public int      Id               { get; set; }
    public int      TravelRequestId  { get; set; }
    public string   Category         { get; set; } = string.Empty;  // 交通費、住宿費、餐費、雜支
    public int      SeqNo            { get; set; }                  // 該分類內的項次
    public string   ItemName         { get; set; } = string.Empty;  // 項目說明
    public decimal  UnitPrice        { get; set; }
    public string   Quantity         { get; set; } = string.Empty;  // 數量/單位（如「1式」「2晚」）
    public decimal  TotalPrice       { get; set; }
    public string?  Note             { get; set; }
    public string?  InvoiceNo        { get; set; }   // 發票號碼（OCR，僅假日出差）
    public string?  FileName         { get; set; }   // 上傳檔名
    public string?  FileUrl          { get; set; }   // 檔案 URL
    public DateTime? InvoiceDate     { get; set; }   // 發票日期（OCR）
    public int      SortOrder        { get; set; }

    // Navigation
    public TravelRequest TravelRequest { get; set; } = null!;
}
