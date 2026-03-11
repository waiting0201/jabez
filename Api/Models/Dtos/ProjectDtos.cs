namespace Jabez.Api.Models.Dtos;

public sealed record ProjectDto(
    int      Id,
    string   Code,
    string   Status,
    int?     DepartmentId,
    string?  DepartmentName,
    decimal? BudgetAmount,
    decimal? ActualAmount,
    decimal? BusinessAmount,
    string?  GoogleDriveUrl,
    DateTime CreatedAt);

public sealed record CreateProjectRequest(
    string   Code,
    string?  Status         = null,
    int?     DepartmentId   = null,
    decimal? BudgetAmount   = null,
    decimal? ActualAmount   = null,
    decimal? BusinessAmount = null,
    string?  GoogleDriveUrl = null);

public sealed record UpdateProjectRequest(
    string?  Code,
    string?  Status,
    int?     DepartmentId,
    decimal? BudgetAmount,
    decimal? ActualAmount,
    decimal? BusinessAmount,
    string?  GoogleDriveUrl);
