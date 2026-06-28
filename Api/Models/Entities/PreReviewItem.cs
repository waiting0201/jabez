namespace Jabez.Api.Models.Entities;

public class PreReviewItem
{
    public int     Id                 { get; set; }
    public int     PreReviewRequestId { get; set; }
    public string  FileName           { get; set; } = string.Empty;
    public string? ItemCategory       { get; set; }  // 品項類別（預設值或「其他」自訂文字）
    public decimal Amount             { get; set; }
    public string? ItemName           { get; set; }
    public string? Description        { get; set; }
    public string? Note               { get; set; }
    public string? FileUrl            { get; set; }
    public DateTime? ItemDate         { get; set; }

    // Navigation
    public PreReviewRequest PreReviewRequest { get; set; } = null!;
}
