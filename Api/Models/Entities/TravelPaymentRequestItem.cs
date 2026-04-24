namespace Jabez.Api.Models.Entities;

/// <summary>
/// 出差請款明細：交通費/住宿費/餐費/人事費/雜支 等費用項目。
/// 結構同 TravelRequestItem，FK 指向 TravelPaymentRequest。
/// </summary>
public class TravelPaymentRequestItem
{
    public int      Id                     { get; set; }
    public int      TravelPaymentRequestId { get; set; }
    public string   Category               { get; set; } = string.Empty;  // 交通費、住宿費、餐費、人事費、雜支
    public int      SeqNo                  { get; set; }                  // 該分類內的項次
    public string   ItemName               { get; set; } = string.Empty;  // 項目說明
    public decimal  UnitPrice              { get; set; }
    public string   Quantity               { get; set; } = string.Empty;  // 數量/單位（如「1式」「2晚」）
    public decimal  TotalPrice             { get; set; }
    public string?  Note                   { get; set; }
    public string?  InvoiceNo              { get; set; }   // 發票號碼
    public string?  FileName               { get; set; }   // 上傳檔名
    public string?  FileUrl                { get; set; }   // 檔案 URL
    public DateTime? InvoiceDate           { get; set; }   // 發票日期
    public int      SortOrder              { get; set; }

    // Navigation
    public TravelPaymentRequest TravelPaymentRequest { get; set; } = null!;
}
