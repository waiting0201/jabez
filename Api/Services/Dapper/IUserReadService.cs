using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IUserReadService
{
    Task<IEnumerable<UserDto>>    GetAllAsync();
    Task<PagedResult<UserDto>>    GetPagedAsync(int page, int pageSize);
    Task<UserDto?>                GetByIdAsync(Guid id);
}
