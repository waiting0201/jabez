using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IPaymentReportReadService
{
    Task<PagedResult<PaymentReportDto>> GetPagedAsync(
        int page, int pageSize,
        int? year = null, int? month = null, string? paymentStatus = null);
}
