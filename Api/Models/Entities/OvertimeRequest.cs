namespace Jabez.Api.Models.Entities;

public class OvertimeRequest
{
    public int      Id               { get; set; }
    public Guid?    EmployeeId       { get; set; }
    public int?     ApprovalItemId   { get; set; }
    public DateTime OvertimeDate     { get; set; }
    public decimal  EstimatedHours   { get; set; }   // 合計快取 = SUM(Projects[].EstimatedHours)，由 Handler 重算
    public string   Reason           { get; set; } = string.Empty;
    public string   ApprovalStatus   { get; set; } = "pending";  // pending | approved | rejected | returned
    public int      CurrentStepOrder { get; set; } = 1;
    public Guid?    ReviewedById     { get; set; }
    public DateTime? ReviewedAt      { get; set; }
    public string?  ReviewNote       { get; set; }
    public DateTime CreatedAt        { get; set; }

    // Navigation
    public User?         Employee           { get; set; }
    public User?         ReviewedBy         { get; set; }
    public ApprovalItem? ApprovalItem       { get; set; }

    /// <summary>關聯專案明細（一列一專案，含該專案預估時數）</summary>
    public List<OvertimeRequestProject> Projects { get; set; } = [];
}
