namespace Jabez.Api.Models.Entities;

public class PaymentRequest
{
    public int       Id             { get; set; }
    public string    Type           { get; set; } = string.Empty; // vendor | travel | advance
    public int       ProjectId      { get; set; }
    public int?      ApprovalItemId { get; set; }
    public decimal   TotalAmount    { get; set; }
    public string    ApprovalStatus   { get; set; } = "pending";  // pending | approved | rejected | returned
    public int       CurrentStepOrder { get; set; } = 1;
    public Guid?     SubmittedById  { get; set; }
    public DateTime? ReviewedAt     { get; set; }
    public string?   ReviewNote     { get; set; }
    public Guid?     ReviewedById   { get; set; }
    public DateTime? EstimatedPaymentDate { get; set; }
    public DateTime? PaidAt         { get; set; }
    public Guid?     PaidByUserId   { get; set; }
    public DateTime  CreatedAt      { get; set; }

    // Navigation
    public Project                   Project            { get; set; } = null!;
    public ApprovalItem?             ApprovalItem       { get; set; }
    public User?                     SubmittedBy        { get; set; }
    public User?                     ReviewedBy         { get; set; }
    public User?                     PaidBy             { get; set; }
    public ICollection<InvoiceItem>  InvoiceItems { get; set; } = [];
}
