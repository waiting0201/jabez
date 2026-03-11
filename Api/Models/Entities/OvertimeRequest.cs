namespace Jabez.Api.Models.Entities;

public class OvertimeRequest
{
    public int      Id               { get; set; }
    public Guid?    EmployeeId       { get; set; }
    public int?     ApprovalItemId   { get; set; }
    public DateTime OvertimeDate     { get; set; }
    public string?  ProjectIds       { get; set; }   // 逗號分隔的專案 ID，例如 "1,3,5"
    public decimal  EstimatedHours   { get; set; }
    public string   Reason           { get; set; } = string.Empty;
    public string   ApprovalStatus   { get; set; } = "pending";  // pending | approved | rejected | returned
    public int      CurrentStepOrder { get; set; } = 1;
    public Guid?    ReviewedById     { get; set; }
    public DateTime? ReviewedAt      { get; set; }
    public string?  ReviewNote       { get; set; }
    public DateTime CreatedAt        { get; set; }

    // Navigation
    public User?         Employee     { get; set; }
    public User?         ReviewedBy   { get; set; }
    public ApprovalItem? ApprovalItem { get; set; }
}
