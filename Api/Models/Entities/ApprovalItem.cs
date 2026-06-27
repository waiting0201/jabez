using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

public class ApprovalItem
{
    public int      Id          { get; set; }
    public string   Name        { get; set; } = string.Empty;
    public string   Code        { get; set; } = string.Empty;
    public string?  Description { get; set; }
    public bool     IsActive        { get; set; } = true;
    public string?  ApplicationType { get; set; } // "payment_request" | "leave" | "travel" | "overtime" | null
    public int?     DepartmentId    { get; set; } // null = 該申請類型的通用預設流程；非 null = 某部門專屬流程
    public DateTime CreatedAt       { get; set; } = Clock.Now;

    // Navigation
    public Department?                 Department     { get; set; }
    public ICollection<ApprovalStep>   Steps          { get; set; } = [];
    public ICollection<PaymentRequest> PaymentRequests { get; set; } = [];
    public ICollection<LeaveRequest>   LeaveRequests  { get; set; } = [];
    public ICollection<TravelRequest>   TravelRequests   { get; set; } = [];
    public ICollection<OvertimeRequest>  OvertimeRequests { get; set; } = [];
    public ICollection<AdvanceRequest>   AdvanceRequests  { get; set; } = [];
    public ICollection<TravelPaymentRequest> TravelPaymentRequests { get; set; } = [];
}
