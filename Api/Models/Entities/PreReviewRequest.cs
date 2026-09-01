namespace Jabez.Api.Models.Entities;

public class PreReviewRequest
{
    public int       Id             { get; set; }
    public string?   RequestNo      { get; set; }                 // PRV-yyyyMMdd-NNN；送簽時取號，草稿為 null
    public string    Type           { get; set; } = string.Empty; // vendor | general
    public int       ProjectId      { get; set; }
    public int?      VendorId       { get; set; }
    public int?      ApprovalItemId { get; set; }
    public decimal   TotalAmount    { get; set; }
    public decimal   TaxAmount      { get; set; }
    public string    ApprovalStatus   { get; set; } = "draft";    // draft | pending | approved | rejected | returned
    public int       CurrentStepOrder { get; set; } = 1;
    public Guid?     SubmittedById  { get; set; }
    public DateTime? ReviewedAt     { get; set; }
    public string?   ReviewNote     { get; set; }
    public string?   Reason         { get; set; }
    public Guid?     ReviewedById   { get; set; }
    public DateTime  CreatedAt      { get; set; }

    // Navigation
    public Project                               Project      { get; set; } = null!;
    public Vendor?                               Vendor       { get; set; }
    public ApprovalItem?                         ApprovalItem { get; set; }
    public User?                                 SubmittedBy  { get; set; }
    public User?                                 ReviewedBy   { get; set; }
    public ICollection<PreReviewItem>            Items        { get; set; } = [];
    public ICollection<PreReviewRequestAttachment> Attachments { get; set; } = [];
}
