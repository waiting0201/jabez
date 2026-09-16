using System.Security.Claims;
using Jabez.Api.Common;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// GET /reports/overtime → 加班紀錄報表（已核准的加班申請 + 實際打卡時數）
///
/// 欄位級權限：「加班費」金額另需 reports-overtime:amount，
/// 沒有該碼者仍可進頁面看時數與補償方式，只是金額不回傳。
/// 本端點是唯一能以「全公司逐筆」形式看到他人加班費的地方（其餘皆為看自己 / 看指派給自己的單，
/// 或已受更嚴格的 payroll:read 管制）。
/// </summary>
public sealed class OvertimeReportHandler(IOvertimeReportReadService reader, IProjectAccessResolver access)
{
    /// <summary>
    /// 匯出模式的 pageSize 上限（比照出缺勤報表的 AttendanceLeaveMerger.ExportMaxPageSize）。
    /// 一般列表仍鎖在 100 —— 放寬只給 export=true，且仍是顯式常數而非前端任意值。
    /// </summary>
    public const int ExportMaxPageSize = 5000;

    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        // 匯出模式放寬 pageSize 上限：前端 exportExcel() 要一次帶回整段區間，
        // 而一般列表的 100 上限會把它靜默截斷成前 100 筆（2026-09 修正）。
        bool isExport = req.Query["export"] == "true";
        int  maxSize  = isExport ? ExportMaxPageSize : 100;

        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)             : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, maxSize) : 20;

        Guid? employeeId   = Guid.TryParse(req.Query["employeeId"], out var eid) ? eid : null;
        int?  projectId    = int.TryParse(req.Query["projectId"],   out var pid) ? pid : null;
        DateOnly? dateFrom = DateOnly.TryParse(req.Query["dateFrom"], out var df) ? df : null;
        DateOnly? dateTo   = DateOnly.TryParse(req.Query["dateTo"],   out var dt) ? dt : null;

        var scope = await access.ResolveAsync(req.HttpContext.User);
        var result = await reader.GetPagedAsync(scope, page, pageSize, employeeId, projectId, dateFrom, dateTo);

        // 無 reports-overtime:amount 者抹除加班費金額。ReadService / SQL 不動（Dapper 只管查、Handler 管授權）。
        // PagedResult<T> 是 record 且 Items 為 IEnumerable —— 必須連外層一起 with 重建，並即時求值。
        // CompensationType 刻意保留：它是流程資訊不含金額，且反推金額需要時薪快照 → 底薪，
        // 而底薪已受 payroll:read 管制，反推鏈不成立。
        if (!CanSeeAmount(req.HttpContext.User))
            result = result with
            {
                Items = result.Items.Select(r => r with { OvertimePayAmount = null }).ToList()
            };

        return new OkObjectResult(ApiResponse.Ok(result));
    }

    /// <summary>是否可看「加班費」金額：Superadmin 全通過，否則需持有 reports-overtime:amount。</summary>
    private static bool CanSeeAmount(ClaimsPrincipal user)
    {
        if (string.Equals(user.FindFirst("is_superadmin")?.Value, "true", StringComparison.OrdinalIgnoreCase))
            return true;
        return user.FindAll("permissions").Any(c => c.Value == PermissionCodes.ReportsOvertimeAmount);
    }
}
