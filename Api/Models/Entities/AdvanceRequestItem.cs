namespace Jabez.Api.Models.Entities;

public class AdvanceRequestItem
{
    public int      Id                { get; set; }
    public int      AdvanceRequestId  { get; set; }
    public int      RoundNo           { get; set; } = 1;             // 所屬預支批次（1 = 原始預支，≥2 = 第N次追加）
    public string   Category          { get; set; } = string.Empty;  // 交通費, 活動費, 設計費, 雜支 …
    public int      SeqNo             { get; set; }                  // 該分類內的項次
    public string   ItemName          { get; set; } = string.Empty;  // 項目說明
    public decimal  UnitPrice         { get; set; }
    public string   Quantity          { get; set; } = string.Empty;  // 數量/單位（如「1式」「30小時」）
    public decimal  TotalPrice        { get; set; }
    public decimal  CashAmount        { get; set; }                  // 現金（預支）
    public decimal  CheckAmount       { get; set; }                  // 支票（月結算）
    public string?  Note              { get; set; }
    public int      SortOrder         { get; set; }
    public string?  FileName          { get; set; }                  // 原始檔名
    public string?  FileUrl           { get; set; }                  // Azure Blob Storage URL

    // Navigation
    public AdvanceRequest AdvanceRequest { get; set; } = null!;
}
