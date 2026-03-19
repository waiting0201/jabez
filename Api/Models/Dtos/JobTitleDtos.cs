namespace Jabez.Api.Models.Dtos;

public sealed record JobTitleDto(
    int      Id,
    string   Name,
    int      Level,
    string?  Description,
    int      EmployeeCount,
    DateTime CreatedAt);

/// <summary>輕量級職稱資料（供下拉選單用，不需 job-titles:read 權限）</summary>
public sealed record JobTitleLookupDto(
    int    Id,
    string Name);

public sealed record CreateJobTitleRequest(
    string  Name,
    int     Level,
    string? Description = null);

public sealed record UpdateJobTitleRequest(
    string?  Name,
    int?     Level,
    string?  Description);
