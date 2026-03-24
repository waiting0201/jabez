using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface ITravelWriteOffRequestReadService
{
    Task<PagedResult<TravelWriteOffRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null);
    Task<TravelWriteOffRequestDto?> GetByIdAsync(int id);
}
