using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface ILeaveRequestReadService
{
    Task<IEnumerable<LeaveRequestDto>>    GetAllAsync();
    Task<PagedResult<LeaveRequestDto>>    GetPagedAsync(int page, int pageSize, Guid? userId = null);
    Task<LeaveRequestDto?>                GetByIdAsync(int id);
}
