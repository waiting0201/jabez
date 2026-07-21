namespace Jabez.Api.Models.Entities;

public class LeaveRequest
{
    public int      Id             { get; set; }
    public Guid?    EmployeeId     { get; set; }
    public int?     ApprovalItemId { get; set; }
    public string   LeaveType      { get; set; } = string.Empty; // annual | personal | sick | compensatory | marriage | bereavement | official | maternity | miscarriage_3m | miscarriage_2to3m | miscarriage_under2m | prenatal_checkup | paternity
    public DateTime StartDate      { get; set; }
    public DateTime EndDate        { get; set; }
    public decimal  Hours          { get; set; }
    public string   Reason         { get; set; } = string.Empty;
    public string   ApprovalStatus   { get; set; } = "pending";  // pending | approved | rejected | returned
    public int      CurrentStepOrder { get; set; } = 1;
    public Guid?    ReviewedById   { get; set; }
    public DateTime? ReviewedAt   { get; set; }
    public string?  ReviewNote     { get; set; }
    public string?  BereavementRelationship { get; set; }  // 喪假親屬關係
    public Guid?    AgentUserId    { get; set; }  // 職務代理人（記錄 + 通知，不參與簽核）
    public DateTime CreatedAt      { get; set; }

    // Navigation
    public User?         Employee           { get; set; }
    public User?         ReviewedBy         { get; set; }
    public User?         AgentUser          { get; set; }
    public ApprovalItem? ApprovalItem       { get; set; }
}
