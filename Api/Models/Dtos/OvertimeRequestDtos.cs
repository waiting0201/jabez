namespace Jabez.Api.Models.Dtos;

public sealed record OvertimeRequestDto(
    int       Id,
    string    EmployeeName,
    DateTime  OvertimeDate,
    int[]?    ProjectIds,
    string[]? ProjectCodes,
    string[]? ProjectNames,
    decimal   EstimatedHours,
    string    Reason,
    string    ApprovalStatus,
    DateTime  CreatedAt,
    DateTime? ReviewedAt,
    string?   ReviewNote,
    int?      ApprovalItemId       = null,
    int?      CurrentStepOrder     = null,
    Guid?     ReviewedById         = null,
    DesignatedReviewerDto[]? DesignatedReviewers = null);

public sealed record CreateOvertimeRequestRequest(
    Guid?    EmployeeId,
    int?     ApprovalItemId       = null,
    DateTime OvertimeDate         = default,
    int[]?   ProjectIds           = null,
    decimal  EstimatedHours       = 1m,
    string   Reason               = "",
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

public sealed record UpdateOvertimeRequestRequest(
    DateTime? OvertimeDate,
    int[]?    ProjectIds,
    decimal?  EstimatedHours,
    string?   Reason,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);
