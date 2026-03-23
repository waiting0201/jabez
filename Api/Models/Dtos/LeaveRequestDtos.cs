namespace Jabez.Api.Models.Dtos;

public sealed record LeaveRequestDto(
    int       Id,
    string    EmployeeName,
    string    LeaveType,
    DateTime  StartDate,
    DateTime  EndDate,
    decimal   Hours,
    string    Reason,
    string    ApprovalStatus,
    DateTime  CreatedAt,
    DateTime? ReviewedAt,
    string?   ReviewNote,
    int?      ApprovalItemId       = null,
    int?      CurrentStepOrder     = null,
    Guid?     ReviewedById         = null,
    string?   BereavementRelationship = null,
    DesignatedReviewerDto[]? DesignatedReviewers = null);

public sealed record CreateLeaveRequestRequest(
    Guid?    EmployeeId,
    int?     ApprovalItemId       = null,
    string   LeaveType            = "annual",
    DateTime StartDate            = default,
    DateTime EndDate              = default,
    decimal  Hours                = 1m,
    string   Reason               = "",
    string?  BereavementRelationship = null,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

public sealed record UpdateLeaveRequestRequest(
    string?   LeaveType,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal?  Hours,
    string?   Reason,
    string?   BereavementRelationship = null,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);
