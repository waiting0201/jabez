namespace Jabez.Api.Models.Entities;

/// <summary>
/// 銷假申請（已核准請假單的取消）。
///
/// 設計要點：
/// - 為「獨立子單」而非掛回父單的批次（對照追加預支 AdvanceRequestSupplement 的 RoundNo + Prev* 快照）。
///   父單 LeaveRequest 在銷假送簽期間**完全不動**（維持 approved），故打卡阻擋 / 額度佔用 / 重疊驗證
///   全部自動維持「仍在請假中」語意；銷假被拒也不需要任何回滾。
/// - 簽核流程「跑原本的請假簽核一次」：ApprovalItem 以 "leave" 解析（複用請假流程設定），
///   但 ApprovalRecord / RequestDesignatedReviewer / 簽核任務一律以 "leave_revocation" 為 applicationType，
///   避免與同 Id 的 LeaveRequest 在「此人已審過」查詢中撞號。
/// - 取消哪幾天由 <see cref="Dates"/> 表達，支援挖空中間日的部分銷假。
/// </summary>
public class LeaveRevocation
{
    public int      Id             { get; set; }
    public int      LeaveRequestId { get; set; }
    public Guid?    EmployeeId     { get; set; }
    public string   Reason         { get; set; } = string.Empty;
    /// <summary>本次銷假的時數合計（= Dates 各日時數總和）</summary>
    public decimal  RevokedHours   { get; set; }
    public string   ApprovalStatus   { get; set; } = "draft";   // draft | pending | approved | rejected | returned
    public int?     ApprovalItemId   { get; set; }
    public int      CurrentStepOrder { get; set; } = 1;
    public Guid?    ReviewedById   { get; set; }
    public DateTime? ReviewedAt    { get; set; }
    public string?  ReviewNote     { get; set; }
    public DateTime CreatedAt      { get; set; }
    public DateTime? SubmittedAt   { get; set; }                  // 送簽日期；草稿為 null，送簽當下寫入，退回重送不改

    // Navigation
    public LeaveRequest?              LeaveRequest { get; set; }
    public User?                      Employee     { get; set; }
    public User?                      ReviewedBy   { get; set; }
    public ApprovalItem?              ApprovalItem { get; set; }
    public ICollection<LeaveRevocationDate> Dates  { get; set; } = [];
}
