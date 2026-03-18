using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IProjectReadService
{
    Task<IEnumerable<ProjectDto>>    GetAllAsync();
    Task<IEnumerable<ProjectDto>>    GetActiveAsync();
    Task<PagedResult<ProjectDto>>    GetPagedAsync(int page, int pageSize, string? search = null, int? year = null, string? status = null);
    Task<IEnumerable<int>>           GetYearsAsync();
    Task<ProjectDto?>                GetByIdAsync(int id);
}
