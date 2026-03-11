using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface ITravelRequestReadService
{
    Task<IEnumerable<TravelRequestDto>>    GetAllAsync();
    Task<PagedResult<TravelRequestDto>>    GetPagedAsync(int page, int pageSize, Guid userId);
    Task<TravelRequestDto?>                GetByIdAsync(int id);
}
