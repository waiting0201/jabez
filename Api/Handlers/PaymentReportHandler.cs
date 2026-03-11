using Jabez.Api.Common;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// GET /reports/payment → 請款統計報表
/// </summary>
public sealed class PaymentReportHandler(IPaymentReportReadService reader)
{
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        int? year    = int.TryParse(req.Query["year"],  out var y) ? y : null;
        int? month   = int.TryParse(req.Query["month"], out var m) ? m : null;
        string? paymentStatus = req.Query["paymentStatus"];
        if (string.IsNullOrEmpty(paymentStatus)) paymentStatus = null;

        var result = await reader.GetPagedAsync(page, pageSize, year, month, paymentStatus);
        return new OkObjectResult(ApiResponse.Ok(result));
    }
}
