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
            var result = await reader.GetPagedAsync(page, pageSize);
            return new OkObjectResult(ApiResponse.Ok(result));
        }

        var all = await reader.GetAllAsync();
        return new OkObjectResult(ApiResponse.Ok(all));
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

        if (await db.Projects.AnyAsync(p => p.Code == body.Code))
            throw AppException.Conflict($"Project code '{body.Code}' is already in use.");

        var project = new Project
        {
            Code           = body.Code.Trim(),
            Status         = body.Status ?? "active",
            DepartmentId   = body.DepartmentId,
            BudgetAmount   = body.BudgetAmount,
            ActualAmount   = body.ActualAmount,
            BusinessAmount = body.BusinessAmount,
            GoogleDriveUrl = body.GoogleDriveUrl,
            CreatedAt      = Clock.Now,
        };
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
        if (body.Status is not null)       project.Status         = body.Status;
        if (body.DepartmentId.HasValue)     project.DepartmentId   = body.DepartmentId == 0 ? null : body.DepartmentId;
        if (body.BudgetAmount.HasValue)     project.BudgetAmount   = body.BudgetAmount;
        if (body.ActualAmount.HasValue)     project.ActualAmount   = body.ActualAmount;
        if (body.BusinessAmount.HasValue)   project.BusinessAmount = body.BusinessAmount;
        if (body.GoogleDriveUrl is not null) project.GoogleDriveUrl = body.GoogleDriveUrl;

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
}
