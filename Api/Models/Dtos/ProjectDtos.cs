namespace Jabez.Api.Models.Dtos;

public sealed record ProjectDto(
    int      Id,
    string   Code,
    string   Name,
    string   Status,
    DateTime StartDate,
    DateTime? EndDate,
    int?     DepartmentId,
    string?  DepartmentName,
    decimal? BudgetAmount,
    decimal? ActualAmount,
    decimal? BusinessAmount,
    string?  GoogleDriveUrl,
    DateTime CreatedAt);

public sealed record CreateProjectRequest(
    string   Code,
    string   Name,
    DateTime StartDate,
    DateTime? EndDate        = null,
    string?  Status         = null,
    int?     DepartmentId   = null,
    decimal? BudgetAmount   = null,
    decimal? ActualAmount   = null,
    decimal? BusinessAmount = null,
    string?  GoogleDriveUrl = null);

public sealed record UpdateProjectRequest(
    string?   Code,
    string?   Name,
    string?   Status,
    DateTime? StartDate,
    DateTime? EndDate,
    int?      DepartmentId,
    decimal?  BudgetAmount,
    decimal?  ActualAmount,
    decimal?  BusinessAmount,
    string?   GoogleDriveUrl);
