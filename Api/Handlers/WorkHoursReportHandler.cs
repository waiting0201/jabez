using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// GET /reports/work-hours?dateFrom=&amp;dateTo=&amp;departmentId=&amp;projectId=       → 工時總表（員工 × 專案，不分頁）
/// GET /reports/work-hours/daily?...（同參數）                                       → 逐日明細（供 Excel 第二張工作表）
///
/// 權限 reports-work-hours:read（路由層）；部門可見性由 IProjectAccessResolver 套在 u.DepartmentId。
///
/// 計算規則（見 docs/business/project-work-hours.md）：
///   · 母體＝區間內有下班時間的打卡紀錄；加班不併入。
///   · 工時一律「每天 <see cref="HoursPerDay"/> 小時 × 佔比 ÷ 100」，**不看實際上下班時間、不扣請假**。
///   · 已打下班卡卻沒有專案分配（系統自動補下班卡、忘記填）→ 整天 8 小時算進「未分配」（ProjectId = null）。
///   · 帶 projectId 時不列未分配。
/// 佔比最多 2 位小數、× 8 ÷ 100 後最多 4 位小數，decimal 精確，故彙總不需捨入。
/// </summary>
public sealed class WorkHoursReportHandler(
    IWorkHoursReportReadService reader,
    IProjectAccessResolver access,
    IProjectAllocationSchedule allocationSchedule)
{
    /// <summary>每個工作天計入的工時（小時）。專案工時 ＝ HoursPerDay × 佔比% ÷ 100。</summary>
    public const decimal HoursPerDay = 8m;

    /// <summary>查詢區間跨度上限（天），比照出缺勤報表 / 補休總表。</summary>
    public const int MaxRangeDays = 400;

    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        var raws = await LoadAsync(req);

        var rows = raws
            .GroupBy(r => (r.EmployeeId, r.ProjectId))
            .Select(g =>
            {
                var first = g.First();
                return new WorkHoursRowDto(
                    first.DepartmentName, first.EmployeeId, first.EmployeeName,
                    first.ProjectId, first.ProjectCode, first.ProjectName,
                    g.Sum(HoursOf));
            })
            .OrderBy(r => r.ProjectId is null ? 1 : 0)                       // 未分配排最後
            .ThenBy(r => r.ProjectCode, ProjectCodeComparer.Instance)
            .ThenBy(r => r.DepartmentName, StringComparer.CurrentCulture)
            .ThenBy(r => r.EmployeeName, StringComparer.CurrentCulture)
            .ThenBy(r => r.EmployeeId)
            .ToList();

        return new OkObjectResult(ApiResponse.Ok(rows));
    }

    public async Task<IActionResult> GetDailyAsync(HttpRequest req)
    {
        var raws = await LoadAsync(req);

        // ReadService 已依 日期 → 員工姓名 → Id → SortOrder 排序，這裡原序轉換即可
        var rows = raws
            .Select(r => new WorkHoursDailyRowDto(
                r.Date.ToString("yyyy-MM-dd"), r.DepartmentName, r.EmployeeId, r.EmployeeName,
                r.ProjectId, r.ProjectCode, r.ProjectName,
                r.Percentage, HoursOf(r), r.IsAutoClockOut))
            .ToList();

        return new OkObjectResult(ApiResponse.Ok(rows));
    }

    /// <summary>單列工時：有分配 ＝ 8 × 佔比 ÷ 100；未分配 ＝ 整天 8 小時。</summary>
    private static decimal HoursOf(WorkHoursRaw r) =>
        r.Percentage is { } pct ? HoursPerDay * pct / 100m : HoursPerDay;

    private async Task<IReadOnlyList<WorkHoursRaw>> LoadAsync(HttpRequest req)
    {
        var today = Clock.Now.Date;
        var dateFrom = DateTime.TryParse(req.Query["dateFrom"], out var df) ? df.Date : new DateTime(today.Year, today.Month, 1);
        var dateTo   = DateTime.TryParse(req.Query["dateTo"],   out var dt) ? dt.Date : dateFrom.AddMonths(1).AddDays(-1);

        if (dateTo < dateFrom)
            throw AppException.BadRequest("迄日不得早於起日。");
        if ((dateTo - dateFrom).TotalDays > MaxRangeDays)
            throw AppException.BadRequest($"查詢區間不得超過 {MaxRangeDays} 天。");

        int? departmentId = int.TryParse(req.Query["departmentId"], out var did) ? did : null;
        int? projectId    = int.TryParse(req.Query["projectId"],    out var pid) ? pid : null;

        // 專案工時上線前無資料；上線後只算上線日（含）之後，之前的紀錄本來就沒有佔比、全列未分配沒有意義
        if (allocationSchedule.StartDate is not { } start || start > today)
            return [];
        if (dateFrom < start) dateFrom = start;
        if (dateTo < dateFrom) return [];

        var scope = await access.ResolveAsync(req.HttpContext.User);
        return await reader.GetRowsAsync(scope, dateFrom, dateTo, departmentId, projectId);
    }

    /// <summary>
    /// 專案代號自然排序（與 ProjectReadService.NaturalCodeOrder 同語意）：
    /// 英文前綴升冪 → 前綴後第一段連續數字（數值）升冪 → Code。J11203-T → J / 11203；C005 → C / 5。
    /// </summary>
    private sealed class ProjectCodeComparer : IComparer<string?>
    {
        public static readonly ProjectCodeComparer Instance = new();

        public int Compare(string? x, string? y)
        {
            if (x is null || y is null) return (x is null ? 0 : 1) - (y is null ? 0 : 1);

            var (px, nx) = Split(x);
            var (py, ny) = Split(y);
            int c = string.Compare(px, py, StringComparison.OrdinalIgnoreCase);
            if (c != 0) return c;
            c = nx.CompareTo(ny);
            return c != 0 ? c : string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }

        private static (string Prefix, decimal Number) Split(string code)
        {
            int i = 0;
            while (i < code.Length && !char.IsAsciiDigit(code[i])) i++;
            int j = i;
            while (j < code.Length && char.IsAsciiDigit(code[j])) j++;
            // 無數字者數值 0；超長數字串 TryParse 失敗亦退回 0（SQL 端 TRY_CAST 失敗為 NULL，同屬最前）
            decimal.TryParse(code.AsSpan(i, j - i), out var n);
            return (code[..i], n);
        }
    }
}
