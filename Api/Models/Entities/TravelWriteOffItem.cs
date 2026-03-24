namespace Jabez.Api.Models.Entities;

public class TravelWriteOffItem
{
    public int      Id                     { get; set; }
    public int      TravelWriteOffRecordId { get; set; }
    public string   Category               { get; set; } = string.Empty;
    public int      SeqNo                  { get; set; }
    public string   ItemName               { get; set; } = string.Empty;
    public decimal  UnitPrice              { get; set; }
    public string   Quantity               { get; set; } = string.Empty;
    public decimal  TotalPrice             { get; set; }
    public string?  Note                   { get; set; }
    public string?  InvoiceNo              { get; set; }
    public string?  FileName               { get; set; }
    public string?  FileUrl                { get; set; }
    public DateTime? InvoiceDate           { get; set; }
    public int      SortOrder              { get; set; }

    // Navigation
    public TravelWriteOffRecord TravelWriteOffRecord { get; set; } = null!;
}
