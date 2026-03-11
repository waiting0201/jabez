namespace Jabez.Api.Models.Dtos;

public sealed record DepartmentDto(
    int      Id,
    string   Name,
    string?  Code,
    string?  Description,
    int?     ParentId,
    string?  ParentName,
    int      SortOrder,
    int      EmployeeCount,
    DateTime CreatedAt);

public sealed record CreateDepartmentRequest(
    string  Name,
    string? Code        = null,
    string? Description = null,
    int?    ParentId    = null,
    int     SortOrder   = 0);

public sealed record UpdateDepartmentRequest(
    string?  Name,
    string?  Code,
    string?  Description,
    int?     ParentId,
    int?     SortOrder);
