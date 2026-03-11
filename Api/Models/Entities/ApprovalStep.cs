using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

public class ApprovalStep
{
    public int      Id             { get; set; }
    public int      ApprovalItemId { get; set; }
    public int      StepOrder      { get; set; }
    public int?     DepartmentId   { get; set; }
    public int?     JobTitleId     { get; set; }
    public bool     UseApplicantDepartment { get; set; } = false;
    public string?  Note           { get; set; }
    public DateTime CreatedAt      { get; set; } = Clock.Now;

    // Navigation
    public ApprovalItem  ApprovalItem { get; set; } = null!;
    public Department?   Department   { get; set; }
    public JobTitle?     JobTitle     { get; set; }
}
