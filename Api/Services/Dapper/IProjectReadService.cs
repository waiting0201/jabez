using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IProjectReadService
{
    Task<IEnumerable<ProjectDto>>    GetAllAsync();
    Task<IEnumerable<ProjectDto>>    GetActiveAsync();
    Task<PagedResult<ProjectDto>>    GetPagedAsync(int page, int pageSize);
    Task<ProjectDto?>                GetByIdAsync(int id);
}
