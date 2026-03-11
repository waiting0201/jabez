namespace Jabez.Api.Models.Dtos;

public sealed record JobTitleDto(
    int      Id,
    string   Name,
    int      Level,
    string?  Description,
    int      EmployeeCount,
    DateTime CreatedAt);

public sealed record CreateJobTitleRequest(
    string  Name,
    int     Level,
    string? Description = null);

public sealed record UpdateJobTitleRequest(
    string?  Name,
    int?     Level,
    string?  Description);
