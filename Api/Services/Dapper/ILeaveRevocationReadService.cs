using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface ILeaveRevocationReadService
{
    Task<PagedResult<LeaveRevocationDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null);
    Task<LeaveRevocationDto?> GetByIdAsync(int id);
}
