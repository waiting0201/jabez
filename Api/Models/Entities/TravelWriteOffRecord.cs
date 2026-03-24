namespace Jabez.Api.Models.Entities;

public class TravelWriteOffRecord
{
    public int       Id               { get; set; }
    public string    RequestNo        { get; set; } = string.Empty; // TWO-yyyyMMdd-NNN
    public int       TravelRequestId  { get; set; }
    public int       WriteOffNo       { get; set; }   // 第幾次沖銷（1, 2, 3…）
    public decimal   GrandTotal       { get; set; }
    public string?   Note             { get; set; }
    public Guid?     SubmittedById    { get; set; }
    public DateTime  CreatedAt        { get; set; }

    // 簽核流程欄位
    public int?      ApprovalItemId   { get; set; }
    public string    ApprovalStatus   { get; set; } = "draft";
    public int       CurrentStepOrder { get; set; } = 1;
    public DateTime? ReviewedAt       { get; set; }
    public Guid?     ReviewedById     { get; set; }
    public string?   ReviewNote       { get; set; }

    // Navigation
    public TravelRequest                      TravelRequest  { get; set; } = null!;
    public User?                              SubmittedBy    { get; set; }
    public ApprovalItem?                      ApprovalItem   { get; set; }
    public User?                              ReviewedBy     { get; set; }
    public ICollection<TravelWriteOffItem>    Items          { get; set; } = [];
}
