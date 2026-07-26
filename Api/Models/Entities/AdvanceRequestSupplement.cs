namespace Jabez.Api.Models.Entities;

/// <summary>
/// 追加預支批次（RoundNo ≥ 2）。Round 1 = AdvanceRequest 本身（AdvanceDate + RoundNo=1 的 Items），故不入此表。
/// 各批次金額一律由 SUM(AdvanceRequestItems WHERE RoundNo = N) 推導，此表不存金額快取。
/// </summary>
public class AdvanceRequestSupplement
{
    public int       Id               { get; set; }
    public int       AdvanceRequestId { get; set; }
    public int       RoundNo          { get; set; }                  // ≥ 2
    public DateTime  AdvanceDate      { get; set; }                  // 該批次的預支日期
    public string?   Reason           { get; set; }                  // 追加原因
    public Guid?     CreatedById      { get; set; }
    public DateTime  CreatedAt        { get; set; }

    // 駁回回滾快照：追加被拒絕時把父單還原成「送出追加之前」的核准狀態
    public int       PrevCurrentStepOrder { get; set; }
    public DateTime? PrevReviewedAt       { get; set; }
    public Guid?     PrevReviewedById     { get; set; }
    public string?   PrevReviewNote       { get; set; }

    // Navigation
    public AdvanceRequest AdvanceRequest  { get; set; } = null!;
    public User?          CreatedBy       { get; set; }
    public User?          PrevReviewedBy  { get; set; }
}
