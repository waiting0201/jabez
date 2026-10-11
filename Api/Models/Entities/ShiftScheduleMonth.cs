namespace Jabez.Api.Models.Entities;

/// <summary>
/// 個人排班的「整月狀態」—— 供「該員次月是否已完成排班」「是否為系統自動排班」的判定，
/// 不必每次都去 count ShiftScheduleDay。
///
/// 消費點：20 號未完成提醒（LINE）、25 號截止後的逾期判定、26 號自動排班批次、
/// 改班申請的「班表是否已定案」閘門。
/// </summary>
public class ShiftScheduleMonth
{
    public int  Id     { get; set; }
    public Guid UserId { get; set; }
    public int  Year   { get; set; }
    public int  Month  { get; set; }

    /// <summary>draft（暫存未通過配額）/ committed（已完成）/ auto（系統自動排班產生）。</summary>
    public string Status { get; set; } = ShiftScheduleMonthStatus.Draft;

    /// <summary>通過配額檢核並完成儲存的時間。</summary>
    public DateTime? CommittedAt { get; set; }

    /// <summary>被 26 號批次自動排班的時間；非 null 即代表該月班表非本人所排。</summary>
    public DateTime? AutoAssignedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User? User { get; set; }
}

public static class ShiftScheduleMonthStatus
{
    /// <summary>暫存：期間內可改，但尚未通過「4 例 ＋ 關卡 A ＋ 關卡 B」擋存判準。</summary>
    public const string Draft     = "draft";
    /// <summary>已完成：通過擋存判準並儲存。</summary>
    public const string Committed = "committed";
    /// <summary>系統自動排班（26 號批次）產生。</summary>
    public const string Auto      = "auto";
}
