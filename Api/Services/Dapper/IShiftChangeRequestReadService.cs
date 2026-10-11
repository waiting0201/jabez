using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IShiftChangeRequestReadService
{
    Task<PagedResult<ShiftChangeRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null);
    Task<ShiftChangeRequestDto?> GetByIdAsync(int id);
}
