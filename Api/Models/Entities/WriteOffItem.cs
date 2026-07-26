namespace Jabez.Api.Models.Entities;

public class WriteOffItem
{
    public int      Id               { get; set; }
    public int      WriteOffRecordId { get; set; }
    public string   Category         { get; set; } = string.Empty;
    public int      SeqNo            { get; set; }
    public string   ItemName         { get; set; } = string.Empty;
    public decimal  UnitPrice        { get; set; }
    public string   Quantity         { get; set; } = string.Empty;
    public decimal  TotalPrice       { get; set; }
    public decimal  CashAmount       { get; set; }
    public decimal  CheckAmount      { get; set; }
    public string?  Note             { get; set; }
    public string?  InvoiceNo        { get; set; }
    public string?  FileName         { get; set; }
    public string?  FileUrl          { get; set; }
    public DateTime? InvoiceDate     { get; set; }
    public int      SortOrder        { get; set; }

    // 支票支付註記（支票由公司直接付給廠商，非撥款給員工；僅財務體系 / Superadmin 可勾選）
    public bool      CheckPaid       { get; set; }
    public DateTime? CheckPaidAt     { get; set; }
    public Guid?     CheckPaidById   { get; set; }

    // Navigation
    public WriteOffRecord WriteOffRecord { get; set; } = null!;
    public User?          CheckPaidBy    { get; set; }
}
