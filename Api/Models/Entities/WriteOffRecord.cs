namespace Jabez.Api.Models.Entities;

public class WriteOffRecord
{
    public int       Id                { get; set; }
    public string?   RequestNo         { get; set; }                 // WO-yyyyMMdd-NNN；送簽時取號，草稿為 null
    public int       AdvanceRequestId  { get; set; }
    public int       WriteOffNo        { get; set; }   // 第幾次沖銷（1, 2, 3…）
    public decimal   CashTotal         { get; set; }
    public decimal   CheckTotal        { get; set; }
    public decimal   GrandTotal        { get; set; }
    public string?   Note              { get; set; }
    public Guid?     SubmittedById     { get; set; }
    public DateTime  CreatedAt         { get; set; }

    // 簽核流程欄位
    public int?      ApprovalItemId    { get; set; }
    public string    ApprovalStatus    { get; set; } = "draft";
    public int       CurrentStepOrder  { get; set; } = 1;
    public DateTime? ReviewedAt        { get; set; }
    public Guid?     ReviewedById      { get; set; }
    public string?   ReviewNote        { get; set; }

    /// <summary>
    /// 待結案登記：財務於其簽核關卡勾選「預支結案」時設為 true，
    /// 但直到**整張沖銷單轉 approved**才真正寫入 AdvanceRequest.IsClosed
    /// （財務常非最後一關，提前結案會讓總監退回後無法補開沖銷單）。
    /// 退回 / 拒絕時清除，重跑流程需由財務重新勾選。
    /// </summary>
    public bool      PendingClose      { get; set; }

    // Navigation
    public AdvanceRequest             AdvanceRequest { get; set; } = null!;
    public User?                      SubmittedBy    { get; set; }
    public ApprovalItem?              ApprovalItem   { get; set; }
    public User?                      ReviewedBy     { get; set; }
    public ICollection<WriteOffItem>        Items        { get; set; } = [];
    public ICollection<WriteOffAttachment>  Attachments  { get; set; } = [];
    public ICollection<WriteOffInstallment> Installments { get; set; } = [];
}
