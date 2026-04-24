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
    public DateTime CreatedAt       { get; set; } = Clock.Now;

    // Navigation
    public ICollection<ApprovalStep>   Steps          { get; set; } = [];
    public ICollection<PaymentRequest> PaymentRequests { get; set; } = [];
    public ICollection<LeaveRequest>   LeaveRequests  { get; set; } = [];
    public ICollection<TravelRequest>   TravelRequests   { get; set; } = [];
    public ICollection<OvertimeRequest>  OvertimeRequests { get; set; } = [];
    public ICollection<AdvanceRequest>   AdvanceRequests  { get; set; } = [];
    public ICollection<TravelPaymentRequest> TravelPaymentRequests { get; set; } = [];
}
