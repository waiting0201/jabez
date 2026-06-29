using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>
/// 申請單指定審核者：申請人可指定多位審核者依序審核。
/// 一條流程可有多個 UseApplicantDesignated 步驟，每筆 designee 以 ApprovalStepOrder 綁定所屬步驟；
/// 同一步驟內再按 StepOrder 逐一審核。
/// </summary>
public class RequestDesignatedReviewer
{
    public int      Id              { get; set; }
    public string   RequestType     { get; set; } = string.Empty; // payment_request | leave_request | travel_request | overtime_request | advance_request
    public int      RequestId       { get; set; }
    public Guid     ReviewerId      { get; set; }
    public int      ApprovalStepOrder { get; set; } // 所屬 ApprovalStep 的 StepOrder（區分同一申請的多個 designated 步驟）
    public int      StepOrder       { get; set; } // 同一步驟內的審核次序：1, 2, 3...
    public int?     SelectedDepartmentId { get; set; } // 第二步「先選部門→再選人」時申請人選的部門（僅記錄，授權不使用）
    public string   Status          { get; set; } = "pending"; // pending | approved | returned
    public DateTime? ReviewedAt     { get; set; }
    public string?  Comment         { get; set; }
    public DateTime CreatedAt       { get; set; } = Clock.Now;

    // Navigation
    public User Reviewer { get; set; } = null!;
}
