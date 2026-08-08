using System.Security.Claims;
using Jabez.Api.Common;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// GET /reports/project-water-level → 專案水位表
/// 回傳可見範圍內的全部專案（含尚未動支者，水位 0%），
/// 及其已動支金額（＝請款已撥分期 + 已核准預支沖銷 + 出差請款已撥分期 + 已核准出差沖銷）
/// 佔業務執行金額 / 契約金額的百分比。
/// 可見範圍依 CLAUDE.md「部門可見性規則」套用（Superadmin / CanSeeAll 部門看全部；其他員工依 CanViewSiblings / CanViewDescendants 旗標）。
///
/// 欄位級權限：「總專案水位」（分母＝契約金額，含公司保留 40%）另需 reports-project-water-level:total，
/// 沒有該碼者仍可進頁面看業務執行水位，只是總水位相關欄位不回傳。
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

        // 無 reports-project-water-level:total 者抹除總水位相關數值。
        // 連同 PreImportUsedAmount / RemainingAmount 一併抹除 —— 兩者是 TotalPercentage 的分子原料，
        // 留著等同把數字送出去讓前端自己算。ContractAmount 依需求保留。
        if (!CanSeeTotal(req.HttpContext.User))
            result = result
                .Select(r => r with { RemainingAmount = null, PreImportUsedAmount = 0, TotalPercentage = null })
                .ToList();

        return new OkObjectResult(ApiResponse.Ok(result));
    }

    /// <summary>是否可看「總專案水位」：Superadmin 全通過，否則需持有 reports-project-water-level:total。</summary>
    private static bool CanSeeTotal(ClaimsPrincipal user)
    {
        if (string.Equals(user.FindFirst("is_superadmin")?.Value, "true", StringComparison.OrdinalIgnoreCase))
            return true;
        return user.FindAll("permissions").Any(c => c.Value == PermissionCodes.ReportsProjectWaterLevelTotal);
    }
}
