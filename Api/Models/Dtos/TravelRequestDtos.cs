namespace Jabez.Api.Models.Dtos;

public sealed record TravelRequestDto(
    int       Id,
    string    EmployeeName,
    string    Destination,
    DateTime  StartDate,
    DateTime  EndDate,
    decimal   EstimatedCost,
    string    Purpose,
    int?      ProjectId,
    string?   ProjectCode,
    bool      IsHolidayTravel,
    string    ApprovalStatus,  // pending | approved | rejected
    DateTime  CreatedAt,
    DateTime? ReviewedAt,
    string?   ReviewNote);

public sealed record CreateTravelRequestRequest(
    Guid?    EmployeeId,
    int?     ApprovalItemId  = null,
    string   Destination     = "",
    DateTime StartDate       = default,
    DateTime EndDate         = default,
    decimal  EstimatedCost   = 0m,
    string   Purpose         = "",
    int?     ProjectId       = null,
    bool     IsHolidayTravel = false);

public sealed record UpdateTravelRequestRequest(
    string?   Destination,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal?  EstimatedCost,
    string?   Purpose,
    int?      ProjectId,
    bool?     IsHolidayTravel);
