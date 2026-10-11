namespace Jabez.Api.Models.Entities;

/// <summary>
/// 補休扣抵紀錄 —— 一張補休假單可能跨多個 lot（FIFO 逐筆扣抵），故一對多。
/// 「某張補休假到底吃掉哪幾筆加班」的單一真相；到期結算與餘額查詢皆以此為準。
/// </summary>
public class CompensatoryUsage
{
    public int Id    { get; set; }
    public int LotId { get; set; }

    /// <summary>消耗此 lot 的補休假單。</summary>
    public int LeaveRequestId { get; set; }

    /// <summary>本次自該 lot 扣抵的時數。</summary>
    public decimal Hours { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public CompensatoryLot? Lot          { get; set; }
    public LeaveRequest?    LeaveRequest { get; set; }
}
