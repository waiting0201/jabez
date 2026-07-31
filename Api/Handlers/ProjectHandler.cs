using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Handlers;

public sealed class ProjectHandler(AppDbContext db, IProjectReadService reader, IProjectAccessResolver access)
{
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        var scope = await access.ResolveAsync(req.HttpContext.User);

        // 有分頁參數 → 回傳 PagedResult；無分頁參數 → 回傳平面陣列（供下拉選單用）
        if (req.Query.ContainsKey("page") || req.Query.ContainsKey("pageSize"))
        {
            int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
            int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
            string? search = req.Query["search"];
            int? year = int.TryParse(req.Query["year"], out var y) ? y : null;
            string? status = req.Query["status"];
            var result = await reader.GetPagedAsync(scope, page, pageSize, search, year, status);
            return new OkObjectResult(ApiResponse.Ok(result));
        }

        var all = await reader.GetAllAsync(scope);
        return new OkObjectResult(ApiResponse.Ok(all));
    }

    /// <summary>取得所有年度（依 StartDate 年份去重）</summary>
    public async Task<IActionResult> GetYearsAsync()
    {
        var years = await reader.GetYearsAsync();
        return new OkObjectResult(ApiResponse.Ok(years));
    }

    /// <summary>
    /// 取得未結案專案（不需 ProjectsRead 權限；可見範圍依使用者部門過濾，規則見 CLAUDE.md「部門可見性規則」）。
    /// 帶 ?all=true 時不過濾部門，回傳全部未結案專案（供加班申請等跨部門支援情境瀏覽用）。
    /// </summary>
    public async Task<IActionResult> GetActiveAsync(HttpRequest req)
    {
        bool all = string.Equals(req.Query["all"], "true", StringComparison.OrdinalIgnoreCase);
        var scope = all
            ? new ProjectAccessScope(true, Array.Empty<int>())
            : await access.ResolveAsync(req.HttpContext.User);
        var active = await reader.GetActiveAsync(scope);
        return new OkObjectResult(ApiResponse.Ok(active));
    }

    public async Task<IActionResult> GetByIdAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid project ID format."));

        var scope = await access.ResolveAsync(req.HttpContext.User);
        var item = await reader.GetByIdAsync(intId, scope);
        // 不符 scope 一律回 404 避免資訊洩漏
        return item is null
            ? new NotFoundObjectResult(ApiResponse.Fail("Project not found.", $"No project with id '{id}'."))
            : new OkObjectResult(ApiResponse.Ok(item));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<CreateProjectRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (string.IsNullOrWhiteSpace(body.Code))
            return new BadRequestObjectResult(ApiResponse.Fail("Code is required."));

        if (string.IsNullOrWhiteSpace(body.Name))
            return new BadRequestObjectResult(ApiResponse.Fail("Name is required."));

        if (body.DepartmentId <= 0)
            return new BadRequestObjectResult(ApiResponse.Fail("請指定專案所屬部門。"));

        if (!await db.Departments.AnyAsync(d => d.Id == body.DepartmentId))
            return new BadRequestObjectResult(ApiResponse.Fail("指定的部門不存在。"));

        if (await db.Projects.AnyAsync(p => p.Code == body.Code))
            throw AppException.Conflict($"Project code '{body.Code}' is already in use.");

        if (ValidateRemainingAmount(body.RemainingAmount, body.ContractAmount) is { } createErr)
            return new BadRequestObjectResult(ApiResponse.Fail(createErr));

        var project = new Project
        {
            Code            = body.Code.Trim(),
            Name            = body.Name.Trim(),
            Status          = body.Status ?? "active",
            StartDate       = body.StartDate,
            EndDate         = body.EndDate,
            DepartmentId    = body.DepartmentId,
            ContractAmount  = body.ContractAmount,
            BusinessAmount  = body.BusinessAmount,
            RemainingAmount = body.RemainingAmount,
            GoogleDriveUrl  = body.GoogleDriveUrl,
            CreatedAt       = Clock.Now,
        };

        if (body.PaymentSchedules is { Count: > 0 })
        {
            project.PaymentSchedules = body.PaymentSchedules
                .Select((s, idx) => BuildSchedule(s, idx + 1))
                .ToList();
        }

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // 寫入後以 SeeAll scope 讀回，避免寫入者因部門 scope 讀不到自己剛建立/修改的資料
        var dto = await reader.GetByIdAsync(project.Id, new ProjectAccessScope(true, []));
        return new ObjectResult(ApiResponse.Ok(dto, "Project created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid project ID format."));

        var body = await req.ReadFromJsonAsync<UpdateProjectRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var project = await db.Projects.FindAsync(intId)
            ?? throw AppException.NotFound("Project");

        if (project.Status == "closed")
            throw AppException.BadRequest("已結案的專案無法修改。");

        if (body.Code is not null)
        {
            var trimmed = body.Code.Trim();
            if (trimmed != project.Code && await db.Projects.AnyAsync(p => p.Code == trimmed && p.Id != intId))
                throw AppException.Conflict($"Project code '{trimmed}' is already in use.");
            project.Code = trimmed;
        }
        if (body.Name is not null)            project.Name           = body.Name.Trim();
        if (body.Status is not null)          project.Status         = body.Status;
        if (body.StartDate.HasValue)          project.StartDate      = body.StartDate.Value;
        if (body.EndDate.HasValue)            project.EndDate        = body.EndDate;
        if (body.DepartmentId.HasValue)
        {
            if (body.DepartmentId.Value <= 0)
                return new BadRequestObjectResult(ApiResponse.Fail("請指定專案所屬部門。"));
            if (!await db.Departments.AnyAsync(d => d.Id == body.DepartmentId.Value))
                return new BadRequestObjectResult(ApiResponse.Fail("指定的部門不存在。"));
            project.DepartmentId = body.DepartmentId.Value;
        }
        if (body.ContractAmount.HasValue)     project.ContractAmount = body.ContractAmount;
        if (body.BusinessAmount.HasValue)     project.BusinessAmount = body.BusinessAmount;

        // 剩餘金額：以「合併後的契約金額」為比較基準（若本次有送 ContractAmount 用新值，否則用既有值）
        var effectiveContract = body.ContractAmount ?? project.ContractAmount;
        if (body.RemainingAmount.HasValue)
        {
            if (ValidateRemainingAmount(body.RemainingAmount, effectiveContract) is { } updateErr)
                return new BadRequestObjectResult(ApiResponse.Fail(updateErr));
            project.RemainingAmount = body.RemainingAmount;
        }
        if (body.GoogleDriveUrl is not null)  project.GoogleDriveUrl = body.GoogleDriveUrl;

        // 請款期別明細：全量 Replace（刪除舊資料後依 payload 重建）
        if (body.PaymentSchedules is not null)
        {
            var existing = await db.ProjectPaymentSchedules
                .Where(s => s.ProjectId == intId)
                .ToListAsync();
            db.ProjectPaymentSchedules.RemoveRange(existing);

            var fresh = body.PaymentSchedules
                .Select((s, idx) => BuildSchedule(s, idx + 1, intId))
                .ToList();
            if (fresh.Count > 0)
                await db.ProjectPaymentSchedules.AddRangeAsync(fresh);
        }

        await db.SaveChangesAsync();

        // 寫入後以 SeeAll scope 讀回，避免寫入者因部門 scope 讀不到自己剛建立/修改的資料
        var dto = await reader.GetByIdAsync(project.Id, new ProjectAccessScope(true, []));
        return new OkObjectResult(ApiResponse.Ok(dto, "Project updated."));
    }

    public async Task<IActionResult> DeleteAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid project ID format."));

        var project = await db.Projects.FindAsync(intId)
            ?? throw AppException.NotFound("Project");

        if (project.Status == "closed")
            throw AppException.BadRequest("已結案的專案無法刪除。");

        if (await db.PaymentRequests.AnyAsync(pr => pr.ProjectId == intId))
            throw AppException.BadRequest("Cannot delete project with existing payment requests.");

        // OvertimeRequestProjects.ProjectId 是 NO_ACTION 外鍵（雙 FK 子表的第二主檔，見 backend-design.md §7.5）。
        // 此處採「阻擋」而非清洗：明細列被刪會使父表 EstimatedHours 合計快取失真（含已核准單），
        // 故比照上方請款單的做法直接擋下。
        if (await db.OvertimeRequestProjects.AnyAsync(x => x.ProjectId == intId))
            throw AppException.BadRequest("此專案已被加班申請關聯，無法刪除。");

        db.Projects.Remove(project);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Project '{id}' deleted."));
    }

    /// <summary>剩餘金額驗證：負值或大於契約金額皆視為無效。回傳錯誤訊息（null = 通過）</summary>
    private static string? ValidateRemainingAmount(decimal? remaining, decimal? contract)
    {
        if (!remaining.HasValue) return null;
        if (remaining.Value < 0) return "剩餘金額不可為負數。";
        if (contract.HasValue && remaining.Value > contract.Value) return "剩餘金額不可大於契約金額。";
        return null;
    }

    /// <summary>把 DTO 轉成 Entity；若有 ProjectId（更新情境）一併填入</summary>
    private static ProjectPaymentSchedule BuildSchedule(ProjectPaymentScheduleRequest s, int periodNo, int? projectId = null) => new()
    {
        Id            = s.Id is { } gid && gid != Guid.Empty ? gid : Guid.NewGuid(),
        ProjectId     = projectId ?? 0,
        PeriodNo      = periodNo,
        BillingDate   = s.BillingDate,
        BillingAmount = s.BillingAmount,
        InvoiceDate   = s.InvoiceDate,
        InvoiceAmount = s.InvoiceAmount,
        DepositDate   = s.DepositDate,
        DepositAmount = s.DepositAmount,
        DeductionNote = s.DeductionNote,
    };
}
