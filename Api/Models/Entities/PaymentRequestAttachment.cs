namespace Jabez.Api.Models.Entities;

public class PaymentRequestAttachment
{
    public int     Id               { get; set; }
    public int     PaymentRequestId { get; set; }
    public string  FileName         { get; set; } = string.Empty;
    public string? FileUrl          { get; set; }
    public int     SortOrder        { get; set; }

    // Navigation
    public PaymentRequest PaymentRequest { get; set; } = null!;
}
