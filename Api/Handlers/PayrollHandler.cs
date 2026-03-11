using Jabez.Api.Common;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// GET /payroll?year=2026&amp;month=3 → 計算指定月份所有在職員工薪資
/// </summary>
public sealed class PayrollHandler(IPayrollReadService reader)
{
    public async Task<IActionResult> GetMonthlyAsync(HttpRequest req)
    {
        if (!int.TryParse(req.Query["year"], out var year) || year < 2000 || year > 2100)
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的年份參數 (year)。"));

        if (!int.TryParse(req.Query["month"], out var month) || month < 1 || month > 12)
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的月份參數 (month)。"));

        var result = await reader.CalculateMonthlyPayrollAsync(year, month);
        return new OkObjectResult(ApiResponse.Ok(result));
    }
}
