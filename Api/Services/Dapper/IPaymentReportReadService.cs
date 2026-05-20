using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;

namespace Jabez.Api.Services.Dapper;

public interface IPaymentReportReadService
{
    /// <summary>
    /// 款項統計分頁列表。category 必填，6 個合法值：
    /// payment / advance / writeoff / travel-payment / travel / travel-writeoff
    /// </summary>
    Task<PagedResult<PaymentReportDto>> GetPagedAsync(
        ProjectAccessScope scope,
        string category,
        int page, int pageSize,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? paymentStatus = null);

    /// <summary>
    /// 匯出用：主表 LEFT JOIN 子表，一列一明細；前端依 category 對應表頭。
    /// </summary>
    Task<List<PaymentExportRowDto>> GetExportRowsAsync(
        ProjectAccessScope scope,
        string category,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? paymentStatus = null);
}
