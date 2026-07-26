namespace Jabez.Api.Models.Entities;

public class ApprovalRecord
{
    public int      Id              { get; set; }
    public string   ApplicationType { get; set; } = string.Empty; // leave | travel | payment_request
    public int      ApplicationId   { get; set; }
    public int      StepOrder       { get; set; }
    /// <summary>簽核批次（僅 advance 追加預支會 &gt; 1，其餘申請類型恆為 1）</summary>
    public int      RoundNo         { get; set; } = 1;
    public string   Action          { get; set; } = string.Empty; // approved | returned | rejected
    public Guid?    ReviewedById    { get; set; }
    public DateTime ReviewedAt      { get; set; }
    public string?  ReviewNote      { get; set; }

    /// <summary>代理審核：代替哪位原審核者</summary>
    public Guid?    OnBehalfOfUserId { get; set; }
    /// <summary>是否為升級審核（往上層部門找主管）</summary>
    public bool     IsEscalated      { get; set; }

    // Navigation
    public User? ReviewedBy    { get; set; }
    public User? OnBehalfOfUser { get; set; }
}
