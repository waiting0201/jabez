namespace Jabez.Api.Models.Entities;

/// <summary>
/// 出差請款申請：結合出差明細結構（TravelRequest）與財務撥款流程（PaymentRequest）的混合申請單。
/// 不含 IsHolidayTravel、IsClosed、RefundAmount 等沖銷關係欄位，不含參與者。
/// </summary>
public class TravelPaymentRequest
{
    public int      Id              { get; set; }
    public Guid?    EmployeeId      { get; set; }
    public int?     ApprovalItemId  { get; set; }
    public string   Destination     { get; set; } = string.Empty;
    public DateTime StartDate       { get; set; }
    public DateTime EndDate         { get; set; }
    public decimal  GrandTotal      { get; set; }  // SUM(Items.TotalPrice)，由後端自動計算
    public string   Purpose         { get; set; } = string.Empty;
    public int?     ProjectId       { get; set; }
    public string   ApprovalStatus   { get; set; } = "draft";  // draft | pending | approved | rejected | returned
    public int      CurrentStepOrder { get; set; } = 1;
    public Guid?    ReviewedById    { get; set; }
    public DateTime? ReviewedAt     { get; set; }
    public string?  ReviewNote      { get; set; }
    public DateTime CreatedAt       { get; set; }

    // 撥款 cache 欄位（兩階段過渡用，主要資料在 Installments；Handler 寫入時同步更新）
    public DateTime? EstimatedPaymentDate { get; set; }
    public DateTime? PaidAt               { get; set; }
    public Guid?     PaidByUserId         { get; set; }

    // Navigation
    public User?                                         Employee     { get; set; }
    public User?                                         ReviewedBy   { get; set; }
    public User?                                         PaidBy       { get; set; }
    public ApprovalItem?                                 ApprovalItem { get; set; }
    public Project?                                      Project      { get; set; }
    public ICollection<TravelPaymentRequestItem>         Items        { get; set; } = [];
    public ICollection<TravelPaymentRequestInstallment>  Installments { get; set; } = [];
}
