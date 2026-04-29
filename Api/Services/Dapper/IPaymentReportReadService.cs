using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;

namespace Jabez.Api.Services.Dapper;

public interface IPaymentReportReadService
{
    Task<PagedResult<PaymentReportDto>> GetPagedAsync(
        ProjectAccessScope scope,
        int page, int pageSize,
        int? year = null, int? month = null, string? paymentStatus = null);
}
