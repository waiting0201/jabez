namespace Jabez.Api.Models.Entities;

public class InvoiceItem
{
    public int     Id               { get; set; }
    public int     PaymentRequestId { get; set; }
    public string  FileName         { get; set; } = string.Empty;
    public string  InvoiceNo        { get; set; } = string.Empty;
    public decimal Amount           { get; set; }
    public string? ItemName         { get; set; }
    public string? Note             { get; set; }
    public string? FileUrl          { get; set; }
    public DateTime? InvoiceDate    { get; set; }

    // Navigation
    public PaymentRequest PaymentRequest { get; set; } = null!;
}
