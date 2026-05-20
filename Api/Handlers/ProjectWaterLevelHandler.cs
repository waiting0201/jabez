using Jabez.Api.Common;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// GET /reports/project-water-level → 專案水位表
/// 回傳每個有「已動支」金額的專案（已動支 = 請款已撥分期 + 已核准預支沖銷 + 出差請款已撥分期 + 已核准出差沖銷），
/// 及其已動支金額佔業務執行金額 / 契約金額的百分比。
/// 可見範圍依 CLAUDE.md「部門可見性規則」套用（Superadmin / CanSeeAll 部門看全部；其他員工依 CanViewSiblings / CanViewDescendants 旗標）。
/// </summary>
public sealed class ProjectWaterLevelHandler(
    IProjectWaterLevelReadService reader,
    IProjectAccessResolver        access)
{
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        var scope = await access.ResolveAsync(req.HttpContext.User);
        int? year = int.TryParse(req.Query["year"], out var y) ? y : null;
        string? status = req.Query["status"];
        if (string.IsNullOrWhiteSpace(status)) status = null;
        var result = await reader.GetAllAsync(scope, year, status);
        return new OkObjectResult(ApiResponse.Ok(result));
    }
}
