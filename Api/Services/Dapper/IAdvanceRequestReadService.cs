using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IAdvanceRequestReadService
{
    Task<PagedResult<AdvanceRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null);
    Task<AdvanceRequestDto?>             GetByIdAsync(int id);
}
