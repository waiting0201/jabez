namespace Jabez.Api.Models.Entities;

public class WriteOffRecord
{
    public int       Id                { get; set; }
    public int       AdvanceRequestId  { get; set; }
    public int       WriteOffNo        { get; set; }   // 第幾次沖銷（1, 2, 3…）
    public decimal   CashTotal         { get; set; }
    public decimal   CheckTotal        { get; set; }
    public decimal   GrandTotal        { get; set; }
    public string?   Note              { get; set; }
    public Guid?     SubmittedById     { get; set; }
    public DateTime  CreatedAt         { get; set; }

    // Navigation
    public AdvanceRequest             AdvanceRequest { get; set; } = null!;
    public User?                      SubmittedBy    { get; set; }
    public ICollection<WriteOffItem>  Items          { get; set; } = [];
}
