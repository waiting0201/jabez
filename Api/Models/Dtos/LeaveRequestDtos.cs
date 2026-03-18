namespace Jabez.Api.Models.Dtos;

public sealed record LeaveRequestDto(
    int       Id,
    string    EmployeeName,
    string    LeaveType,       // annual | personal | sick | compensatory
    DateTime  StartDate,
    DateTime  EndDate,
    decimal   Hours,
    string    Reason,
    string    ApprovalStatus,  // pending | approved | rejected
    DateTime  CreatedAt,
    DateTime? ReviewedAt,
    string?   ReviewNote,
    Guid?     DesignatedReviewerId   = null,
    string?   DesignatedReviewerName = null);

public sealed record CreateLeaveRequestRequest(
    Guid?    EmployeeId,
    int?     ApprovalItemId       = null,
    string   LeaveType            = "annual",
    DateTime StartDate            = default,
    DateTime EndDate              = default,
    decimal  Hours                = 1m,
    string   Reason               = "",
    Guid?    DesignatedReviewerId = null);

public sealed record UpdateLeaveRequestRequest(
    string?   LeaveType,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal?  Hours,
    string?   Reason,
    Guid?     DesignatedReviewerId = null);
