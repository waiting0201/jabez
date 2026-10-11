namespace Jabez.Api.Models.Entities;

/// <summary>
/// 改班申請（四週彈性工時 §3.5.2）—— 開放期（10–25 日）結束、次月班表定案鎖定後的異動途徑。
///
/// 設計要點：
/// - **核准後才寫入班表**，未核准前原班表完全不動（<see cref="ShiftScheduleDay"/> 不受影響）。
///   故被拒 / 退回都不需要任何回滾，與銷假申請同一套思路。
/// - 唯一不需簽核的例外是「當日 08:30 前調整當天狀態」（臨時調休），由 ShiftScheduleHandler 直接處理。
/// - 改哪幾天、各改成什麼，由 <see cref="Dates"/> 表達 —— 一張單可同時調整多天
///   （例如「12/5 由休假改上班、12/8 由上班改休假」這種對調）。
///
/// ⚠ **與銷假申請的關鍵差異：必須有自己的 ApprovalItem**。
/// 銷假是 <c>ResolveApprovalItemIdAsync("leave", …)</c> 借用請假的流程設定；
/// 改班要的是 §3.5.2 的**逐部門六條簽核路線**，管理員得在〈簽核流程設定〉建得出 6 個 ApprovalItem，
/// 所以 ApplicationType 必須是自己的 "shift_change"。
/// </summary>
public class ShiftChangeRequest
{
    public int      Id         { get; set; }
    /// <summary>SC-yyyyMMdd-NNN；**送簽時取號**，草稿為 null（同全站慣例）</summary>
    public string?  RequestNo  { get; set; }
    public Guid?    EmployeeId { get; set; }

    /// <summary>被異動的班表年月（一張單只處理同一個月）</summary>
    public int      Year       { get; set; }
    public int      Month      { get; set; }

    public string   Reason     { get; set; } = string.Empty;

    public string   ApprovalStatus   { get; set; } = "draft";   // draft | pending | approved | rejected | returned
    public int?     ApprovalItemId   { get; set; }
    public int      CurrentStepOrder { get; set; } = 1;
    public Guid?    ReviewedById { get; set; }
    public DateTime? ReviewedAt  { get; set; }
    public string?  ReviewNote   { get; set; }
    public DateTime CreatedAt    { get; set; }
    /// <summary>送簽日期；草稿為 null，送簽當下與取號同時寫入，退回重送不改</summary>
    public DateTime? SubmittedAt { get; set; }

    // Navigation
    public User?         Employee     { get; set; }
    public User?         ReviewedBy   { get; set; }
    public ApprovalItem? ApprovalItem { get; set; }
    public ICollection<ShiftChangeRequestDate> Dates { get; set; } = [];
}
