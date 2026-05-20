using Jabez.Api.Common;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// GET /reports/payment        → 款項統計報表（分頁）
/// GET /reports/payment/export → 款項統計匯出（不分頁、主表 LEFT JOIN 子表）
/// category 必填，6 個合法值：payment / advance / writeoff / travel-payment / travel / travel-writeoff
/// </summary>
public sealed class PaymentReportHandler(IPaymentReportReadService reader, IProjectAccessResolver access)
{
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        var (category, dateFrom, dateTo, paymentStatus) = ParseFilters(req);

        var scope = await access.ResolveAsync(req.HttpContext.User);
        var result = await reader.GetPagedAsync(scope, category, page, pageSize, dateFrom, dateTo, paymentStatus);
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    public async Task<IActionResult> GetExportAsync(HttpRequest req)
    {
        var (category, dateFrom, dateTo, paymentStatus) = ParseFilters(req);

        var scope = await access.ResolveAsync(req.HttpContext.User);
        var rows = await reader.GetExportRowsAsync(scope, category, dateFrom, dateTo, paymentStatus);
        return new OkObjectResult(ApiResponse.Ok(rows));
    }

    private static (string category, DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus) ParseFilters(HttpRequest req)
    {
        string category = req.Query["category"].ToString();
        if (string.IsNullOrWhiteSpace(category))
            throw AppException.BadRequest("請選擇類別");
        if (!PaymentReportReadService.AllCategories.Contains(category))
            throw AppException.BadRequest("不支援的類別");

        DateOnly? dateFrom = DateOnly.TryParse(req.Query["dateFrom"], out var df) ? df : null;
        DateOnly? dateTo   = DateOnly.TryParse(req.Query["dateTo"],   out var dt) ? dt : null;
        string? paymentStatus = req.Query["paymentStatus"];
        if (string.IsNullOrEmpty(paymentStatus)) paymentStatus = null;
        return (category, dateFrom, dateTo, paymentStatus);
    }
}
