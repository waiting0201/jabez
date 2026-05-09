using Jabez.Api.Common;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// GET /reports/payment        → 請款統計報表（分頁）
/// GET /reports/payment/export → 請款統計匯出（不分頁、一張發票一列）
/// </summary>
public sealed class PaymentReportHandler(IPaymentReportReadService reader, IProjectAccessResolver access)
{
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        var (dateFrom, dateTo, paymentStatus) = ParseFilters(req);

        var scope = await access.ResolveAsync(req.HttpContext.User);
        var result = await reader.GetPagedAsync(scope, page, pageSize, dateFrom, dateTo, paymentStatus);
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    public async Task<IActionResult> GetExportAsync(HttpRequest req)
    {
        var (dateFrom, dateTo, paymentStatus) = ParseFilters(req);

        var scope = await access.ResolveAsync(req.HttpContext.User);
        var rows = await reader.GetExportRowsAsync(scope, dateFrom, dateTo, paymentStatus);
        return new OkObjectResult(ApiResponse.Ok(rows));
    }

    private static (DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus) ParseFilters(HttpRequest req)
    {
        DateOnly? dateFrom = DateOnly.TryParse(req.Query["dateFrom"], out var df) ? df : null;
        DateOnly? dateTo   = DateOnly.TryParse(req.Query["dateTo"],   out var dt) ? dt : null;
        string? paymentStatus = req.Query["paymentStatus"];
        if (string.IsNullOrEmpty(paymentStatus)) paymentStatus = null;
        return (dateFrom, dateTo, paymentStatus);
    }
}
