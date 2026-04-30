using Jabez.Api.Common;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// GET /reports/payment → 請款統計報表
/// </summary>
public sealed class PaymentReportHandler(IPaymentReportReadService reader, IProjectAccessResolver access)
{
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        DateOnly? dateFrom = DateOnly.TryParse(req.Query["dateFrom"], out var df) ? df : null;
        DateOnly? dateTo   = DateOnly.TryParse(req.Query["dateTo"],   out var dt) ? dt : null;
        string? paymentStatus = req.Query["paymentStatus"];
        if (string.IsNullOrEmpty(paymentStatus)) paymentStatus = null;

        var scope = await access.ResolveAsync(req.HttpContext.User);
        var result = await reader.GetPagedAsync(scope, page, pageSize, dateFrom, dateTo, paymentStatus);
        return new OkObjectResult(ApiResponse.Ok(result));
    }
}
