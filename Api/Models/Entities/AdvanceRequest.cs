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
    /// <summary>預支款需求日（選填）：申請人希望款項撥入的日期，供財務排撥款參考。</summary>
    public DateTime? AdvanceNeededDate { get; set; }
    public decimal   CashTotal         { get; set; }
    public decimal   CheckTotal        { get; set; }
    public decimal   GrandTotal        { get; set; }
    public string    ApprovalStatus    { get; set; } = "draft";
    public int       CurrentStepOrder  { get; set; } = 1;
    /// <summary>最新（已建立）的預支批次號。1 = 僅原始預支；&gt; 1 = 已有追加批次。
    /// 「有進行中的追加」＝ CurrentRoundNo &gt; 1 且 ApprovalStatus 為 pending / returned。</summary>
    public int       CurrentRoundNo    { get; set; } = 1;
    public Guid?     SubmittedById     { get; set; }
    public DateTime? ReviewedAt        { get; set; }
    public string?   ReviewNote        { get; set; }
    public Guid?     ReviewedById      { get; set; }

    public DateTime  CreatedAt         { get; set; }

    // 結案欄位
    public bool      IsClosed          { get; set; }
    public DateTime? ClosedAt          { get; set; }
    public Guid?     ClosedById        { get; set; }

    // 退還差額欄位（沖銷累計 > 預支時，系統自動計算）
    public decimal?  RefundAmount         { get; set; }
    public decimal?  RefundedAmount       { get; set; }
    public DateTime? EstimatedRefundDate  { get; set; }
    public DateTime? RefundedAt           { get; set; }
    public Guid?     RefundedByUserId     { get; set; }

    // Navigation
    public Project                                  Project      { get; set; } = null!;
    public ApprovalItem?                            ApprovalItem { get; set; }
    public User?                                    SubmittedBy  { get; set; }
    public User?                                    ReviewedBy   { get; set; }
    public User?                                    ClosedBy     { get; set; }
    public User?                                    RefundedBy   { get; set; }
    public ICollection<AdvanceRequestItem>          Items        { get; set; } = [];
    public ICollection<WriteOffRecord>              WriteOffs    { get; set; } = [];
    public ICollection<AdvanceRequestInstallment>   Installments { get; set; } = [];
    public ICollection<AdvanceRequestSupplement>    Supplements  { get; set; } = [];
}
