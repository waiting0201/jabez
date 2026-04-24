using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;

namespace Jabez.Api.Services.Dapper;

public interface IProjectReadService
{
    Task<IEnumerable<ProjectDto>>    GetAllAsync(ProjectAccessScope scope);
    Task<IEnumerable<ProjectDto>>    GetActiveAsync(ProjectAccessScope scope);
    Task<PagedResult<ProjectDto>>    GetPagedAsync(ProjectAccessScope scope, int page, int pageSize, string? search = null, int? year = null, string? status = null);
    Task<IEnumerable<int>>           GetYearsAsync();
    Task<ProjectDto?>                GetByIdAsync(int id, ProjectAccessScope scope);
}
