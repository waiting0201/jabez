using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;

namespace Jabez.Api.Services.Dapper;

public interface IUserReadService
{
    Task<IEnumerable<UserDto>>    GetAllAsync();
    Task<IEnumerable<UserLookupDto>> GetLookupAsync();
    Task<IEnumerable<UserLookupDto>> GetLookupAsync(ProjectAccessScope scope);
    Task<PagedResult<UserDto>>    GetPagedAsync(int page, int pageSize);
    Task<UserDto?>                GetByIdAsync(Guid id);
}
