namespace Jabez.Api.Models.Entities;

public class TravelRequest
{
    public int      Id              { get; set; }
    public Guid?    EmployeeId      { get; set; }
    public int?     ApprovalItemId  { get; set; }
    public string   Destination     { get; set; } = string.Empty;
    public DateTime StartDate       { get; set; }
    public DateTime EndDate         { get; set; }
    public decimal  EstimatedCost   { get; set; }
    public string   Purpose         { get; set; } = string.Empty;
    public int?     ProjectId       { get; set; }
    public bool     IsHolidayTravel { get; set; }
    public string   ApprovalStatus   { get; set; } = "pending";  // pending | approved | rejected | returned
    public int      CurrentStepOrder { get; set; } = 1;
    public Guid?    ReviewedById    { get; set; }
    public DateTime? ReviewedAt    { get; set; }
    public string?  ReviewNote      { get; set; }
    public Guid?    DesignatedReviewerId { get; set; }
    public DateTime CreatedAt       { get; set; }

    // Navigation
    public User?         Employee           { get; set; }
    public User?         ReviewedBy         { get; set; }
    public User?         DesignatedReviewer { get; set; }
    public ApprovalItem? ApprovalItem       { get; set; }
    public Project?      Project            { get; set; }
}
