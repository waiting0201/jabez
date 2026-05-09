using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;

namespace Jabez.Api.Services.Dapper;

public interface IPaymentReportReadService
{
    Task<PagedResult<PaymentReportDto>> GetPagedAsync(
        ProjectAccessScope scope,
        int page, int pageSize,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? paymentStatus = null);

    /// <summary>
    /// 匯出用：一張發票一列（LEFT JOIN InvoiceItems），不分頁。
    /// </summary>
    Task<List<PaymentExportRowDto>> GetExportRowsAsync(
        ProjectAccessScope scope,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? paymentStatus = null);
}
