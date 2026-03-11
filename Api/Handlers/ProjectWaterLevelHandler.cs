using Jabez.Api.Common;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// GET /reports/project-water-level → 專案水位表
/// 回傳每個有請款紀錄的專案，其請款金額佔業務金額的百分比。
/// </summary>
public sealed class ProjectWaterLevelHandler(IProjectWaterLevelReadService reader)
{
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        var result = await reader.GetAllAsync();
        return new OkObjectResult(ApiResponse.Ok(result));
    }
}
