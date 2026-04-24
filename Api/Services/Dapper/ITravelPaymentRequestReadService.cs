using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface ITravelPaymentRequestReadService
{
    Task<PagedResult<TravelPaymentRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null);
    Task<TravelPaymentRequestDto?>             GetByIdAsync(int id);
}
