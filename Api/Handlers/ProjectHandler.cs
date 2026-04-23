using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Handlers;

public sealed class ProjectHandler(AppDbContext db, IProjectReadService reader)
{
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        // 有分頁參數 → 回傳 PagedResult；無分頁參數 → 回傳平面陣列（供下拉選單用）
        if (req.Query.ContainsKey("page") || req.Query.ContainsKey("pageSize"))
        {
            int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
            int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
            string? search = req.Query["search"];
            int? year = int.TryParse(req.Query["year"], out var y) ? y : null;
            string? status = req.Query["status"];
            var result = await reader.GetPagedAsync(page, pageSize, search, year, status);
            return new OkObjectResult(ApiResponse.Ok(result));
        }

        var all = await reader.GetAllAsync();
        return new OkObjectResult(ApiResponse.Ok(all));
    }

    /// <summary>取得所有年度（依 StartDate 年份去重）</summary>
    public async Task<IActionResult> GetYearsAsync()
    {
        var years = await reader.GetYearsAsync();
        return new OkObjectResult(ApiResponse.Ok(years));
    }

    /// <summary>取得未結案專案（不需 ProjectsRead 權限，供請款/加班表單下拉用）</summary>
    public async Task<IActionResult> GetActiveAsync()
    {
        var active = await reader.GetActiveAsync();
        return new OkObjectResult(ApiResponse.Ok(active));
    }

    public async Task<IActionResult> GetByIdAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid project ID format."));

        var item = await reader.GetByIdAsync(intId);
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

        if (await db.Projects.AnyAsync(p => p.Code == body.Code))
            throw AppException.Conflict($"Project code '{body.Code}' is already in use.");

        var project = new Project
        {
            Code           = body.Code.Trim(),
            Name           = body.Name.Trim(),
            Status         = body.Status ?? "active",
            StartDate      = body.StartDate,
            EndDate        = body.EndDate,
            DepartmentId   = body.DepartmentId,
            ReceivedAmount = body.ReceivedAmount,
            ContractAmount = body.ContractAmount,
            BusinessAmount = body.BusinessAmount,
            GoogleDriveUrl = body.GoogleDriveUrl,
            CreatedAt      = Clock.Now,
        };

        if (body.PaymentSchedules is { Count: > 0 })
        {
            project.PaymentSchedules = body.PaymentSchedules
                .Select((s, idx) => BuildSchedule(s, idx + 1))
                .ToList();
        }

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(project.Id);
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
        if (body.DepartmentId.HasValue)       project.DepartmentId   = body.DepartmentId == 0 ? null : body.DepartmentId;
        if (body.ReceivedAmount.HasValue)     project.ReceivedAmount = body.ReceivedAmount;
        if (body.ContractAmount.HasValue)     project.ContractAmount = body.ContractAmount;
        if (body.BusinessAmount.HasValue)     project.BusinessAmount = body.BusinessAmount;
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

        var dto = await reader.GetByIdAsync(project.Id);
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

        db.Projects.Remove(project);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Project '{id}' deleted."));
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
