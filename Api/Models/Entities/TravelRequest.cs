namespace Jabez.Api.Models.Entities;

public class TravelRequest
{
    public int      Id              { get; set; }
    public string   RequestNo       { get; set; } = string.Empty; // TR-yyyyMMdd-NNN（IsHolidayTravel=true 時為 HTR-yyyyMMdd-NNN）
    public Guid?    EmployeeId      { get; set; }
    public int?     ApprovalItemId  { get; set; }
    public string   Destination     { get; set; } = string.Empty;
    public DateTime StartDate       { get; set; }
    public DateTime EndDate         { get; set; }
    public decimal  GrandTotal      { get; set; }  // SUM(Items.TotalPrice)，由後端自動計算
    public string   Purpose         { get; set; } = string.Empty;
    public int?     ProjectId       { get; set; }
    public bool     IsHolidayTravel { get; set; }
    public int      HolidayDays     { get; set; }  // 假日天數（Submit 時自動計算）
    public string   ApprovalStatus   { get; set; } = "draft";    // draft | pending | approved | rejected | returned
    public int      CurrentStepOrder { get; set; } = 1;
    public Guid?    ReviewedById    { get; set; }
    public DateTime? ReviewedAt    { get; set; }
    public string?  ReviewNote      { get; set; }
    public DateTime CreatedAt       { get; set; }

    // 結案欄位
    public bool      IsClosed    { get; set; }
    public DateTime? ClosedAt    { get; set; }
    public Guid?     ClosedById  { get; set; }

    // 退還差額（沖銷累計超過出差金額時需匯款）
    public decimal?  RefundAmount            { get; set; }
    public decimal?  RefundedAmount          { get; set; }
    public DateTime? EstimatedRefundDate     { get; set; }
    public DateTime? RefundedAt              { get; set; }
    public Guid?     RefundedByUserId        { get; set; }

    // Navigation
    public User?                                  Employee     { get; set; }
    public User?                                  ReviewedBy   { get; set; }
    public User?                                  RefundedBy   { get; set; }
    public ApprovalItem?                          ApprovalItem { get; set; }
    public Project?                               Project      { get; set; }
    public User?                                  ClosedBy     { get; set; }
    public ICollection<TravelRequestItem>         Items        { get; set; } = [];
    public ICollection<TravelRequestParticipant>  Participants { get; set; } = [];
    public ICollection<TravelWriteOffRecord>      WriteOffs    { get; set; } = [];
    public ICollection<TravelRequestInstallment>  Installments { get; set; } = [];
}
