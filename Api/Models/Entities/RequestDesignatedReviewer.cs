using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>
/// 申請單指定審核者：申請人可指定多位審核者依序審核。
/// 同一個 ApprovalStep（UseApplicantDesignated=true）內按 StepOrder 逐一審核。
/// </summary>
public class RequestDesignatedReviewer
{
    public int      Id              { get; set; }
    public string   RequestType     { get; set; } = string.Empty; // payment_request | leave_request | travel_request | overtime_request | advance_request
    public int      RequestId       { get; set; }
    public Guid     ReviewerId      { get; set; }
    public int      StepOrder       { get; set; } // 1, 2, 3...（審核順序）
    public string   Status          { get; set; } = "pending"; // pending | approved | returned
    public DateTime? ReviewedAt     { get; set; }
    public string?  Comment         { get; set; }
    public DateTime CreatedAt       { get; set; } = Clock.Now;

    // Navigation
    public User Reviewer { get; set; } = null!;
}
