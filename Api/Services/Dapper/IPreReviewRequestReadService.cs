using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IPreReviewRequestReadService
{
    Task<IEnumerable<PreReviewRequestDto>> GetAllAsync();
    Task<PagedResult<PreReviewRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null);
    Task<PreReviewRequestDto?>             GetByIdAsync(int id);
}
