namespace Jabez.Api.Models.Entities;

public class AdvanceRequest
{
    public int       Id                { get; set; }
    public string    RequestNo         { get; set; } = string.Empty;
    public int       ProjectId         { get; set; }
    public int?      ApprovalItemId    { get; set; }
    public string    ActivityName      { get; set; } = string.Empty;
    public string    ActivityPeriod    { get; set; } = string.Empty;
    public DateTime  AdvanceDate       { get; set; }
    public decimal   CashTotal         { get; set; }
    public decimal   CheckTotal        { get; set; }
    public decimal   GrandTotal        { get; set; }
    public string    ApprovalStatus    { get; set; } = "draft";
    public int       CurrentStepOrder  { get; set; } = 1;
    public Guid?     SubmittedById     { get; set; }
    public DateTime? ReviewedAt        { get; set; }
    public string?   ReviewNote        { get; set; }
    public Guid?     ReviewedById      { get; set; }
    public DateTime? EstimatedPaymentDate { get; set; }
    public DateTime  CreatedAt         { get; set; }

    // 結案欄位
    public bool      IsClosed          { get; set; }
    public DateTime? ClosedAt          { get; set; }
    public Guid?     ClosedById        { get; set; }

    // 退還差額欄位（沖銷累計 > 預支時，系統自動計算）
    public decimal?  RefundAmount      { get; set; }
    public DateTime? RefundedAt        { get; set; }

    // Navigation
    public Project                          Project            { get; set; } = null!;
    public ApprovalItem?                    ApprovalItem       { get; set; }
    public User?                            SubmittedBy        { get; set; }
    public User?                            ReviewedBy         { get; set; }
    public User?                            ClosedBy           { get; set; }
    public ICollection<AdvanceRequestItem>  Items        { get; set; } = [];
    public ICollection<WriteOffRecord>      WriteOffs    { get; set; } = [];
}
