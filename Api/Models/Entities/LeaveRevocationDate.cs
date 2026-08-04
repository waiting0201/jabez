namespace Jabez.Api.Models.Entities;

/// <summary>
/// 銷假的逐日明細 —— 「哪一天被取消、該日原本幾小時」的單一真相。
/// 下游「某日是否仍在請假中」的查詢一律以「該假單有無 ApprovalStatus='approved' 的銷假涵蓋該日」排除。
/// 各日時數由 <see cref="Jabez.Api.Common.LeaveDayExpander"/> 展開，保證 Σ Hours 與 LeaveRequest.Hours 一致。
/// </summary>
public class LeaveRevocationDate
{
    public int      Id                 { get; set; }
    public int      LeaveRevocationId  { get; set; }
    public DateTime Date               { get; set; }
    public decimal  Hours              { get; set; }

    // Navigation
    public LeaveRevocation? LeaveRevocation { get; set; }
}
