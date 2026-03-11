using Jabez.Api.Common;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// GET /reports/overtime → 加班紀錄報表（已核准的加班申請 + 實際打卡時數）
/// </summary>
public sealed class OvertimeReportHandler(IOvertimeReportReadService reader)
{
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;

        Guid? employeeId = Guid.TryParse(req.Query["employeeId"], out var eid) ? eid : null;
        int?  projectId  = int.TryParse(req.Query["projectId"],   out var pid) ? pid : null;
        int?  year       = int.TryParse(req.Query["year"],  out var y) ? y : null;
        int?  month      = int.TryParse(req.Query["month"], out var m) ? m : null;

        var result = await reader.GetPagedAsync(page, pageSize, employeeId, projectId, year, month);
        return new OkObjectResult(ApiResponse.Ok(result));
    }
}
