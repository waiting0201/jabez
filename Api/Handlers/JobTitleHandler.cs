using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Handlers;

public sealed class JobTitleHandler(AppDbContext db, IJobTitleReadService reader)
{
    // GET /api/job-titles/lookup — 輕量級職稱清單（供下拉選單，不需 job-titles:read 權限）
    public async Task<IActionResult> GetLookupAsync()
    {
        var list = await reader.GetLookupAsync();
        return new OkObjectResult(ApiResponse.Ok(list));
    }

    public async Task<IActionResult> GetAllAsync()
    {
        var titles = await reader.GetAllAsync();
        return new OkObjectResult(ApiResponse.Ok(titles));
    }

    public async Task<IActionResult> GetByIdAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid job title ID format."));

        var title = await reader.GetByIdAsync(intId);
        return title is null
            ? new NotFoundObjectResult(ApiResponse.Fail("Job title not found.", $"No job title with id '{id}'."))
            : new OkObjectResult(ApiResponse.Ok(title));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<CreateJobTitleRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (string.IsNullOrWhiteSpace(body.Name))
            return new BadRequestObjectResult(ApiResponse.Fail("Name is required."));

        var title = new JobTitle
        {
            Name        = body.Name,
            Level       = body.Level,
            Description = body.Description,
            CreatedAt   = Clock.Now,
        };
        db.JobTitles.Add(title);
        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(title.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Job title created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid job title ID format."));

        var body = await req.ReadFromJsonAsync<UpdateJobTitleRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var title = await db.JobTitles.FindAsync(intId)
            ?? throw AppException.NotFound("JobTitle");

        if (body.Name        is not null) title.Name        = body.Name;
        if (body.Level.HasValue)          title.Level       = body.Level.Value;
        if (body.Description is not null) title.Description = body.Description;

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(title.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Job title updated."));
    }

    public async Task<IActionResult> DeleteAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid job title ID format."));

        var title = await db.JobTitles.FindAsync(intId)
            ?? throw AppException.NotFound("JobTitle");

        // 清洗指向 JobTitles 的 NoAction 外鍵（例外指定審核的限定職稱），否則刪除會 FK violation。
        // 清空後該步驟退回「不限職稱」，與 ApprovalStep.JobTitleId 的 SetNull 行為一致。
        await db.ApprovalStepDesignatedJobTitles
            .Where(x => x.JobTitleId == intId)
            .ExecuteDeleteAsync();

        db.JobTitles.Remove(title);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Job title '{id}' deleted."));
    }
}
