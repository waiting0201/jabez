namespace Jabez.Api.Models.Entities;

public class PaymentRequest
{
    public int       Id             { get; set; }
    public string?   RequestNo      { get; set; }                 // PR-yyyyMMdd-NNN；送簽時取號，草稿為 null
    public string    Type           { get; set; } = string.Empty; // vendor | general | business_trip | other
    public int       ProjectId      { get; set; }
    public int?      VendorId       { get; set; }
    public int?      ApprovalItemId { get; set; }
    public decimal   TotalAmount    { get; set; }
    public string    ApprovalStatus   { get; set; } = "draft";    // draft | pending | approved | rejected | returned
    public int       CurrentStepOrder { get; set; } = 1;
    public Guid?     SubmittedById  { get; set; }
    public DateTime? ReviewedAt     { get; set; }
    public string?   ReviewNote     { get; set; }
    public string?   Reason         { get; set; }
    public Guid?     ReviewedById   { get; set; }
    public DateTime  CreatedAt      { get; set; }
    public DateTime? SubmittedAt    { get; set; }                 // 送簽日期；草稿為 null，送簽當下寫入，退回重送不改

    // Navigation
    public Project                                  Project      { get; set; } = null!;
    public Vendor?                                  Vendor       { get; set; }
    public ApprovalItem?                            ApprovalItem { get; set; }
    public User?                                    SubmittedBy  { get; set; }
    public User?                                    ReviewedBy   { get; set; }
    public ICollection<InvoiceItem>                 InvoiceItems { get; set; } = [];
    public ICollection<PaymentRequestInstallment>   Installments { get; set; } = [];
    public ICollection<PaymentRequestAttachment>    Attachments  { get; set; } = [];
}
