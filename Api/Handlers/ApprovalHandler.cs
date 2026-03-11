using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Handlers;

public sealed class ApprovalHandler(AppDbContext db, IApprovalReadService reader)
{
    // ── Approval Items ───────────────────────────────────────────────────

    public async Task<IActionResult> GetAllAsync()
    {
        var items = await reader.GetAllAsync();
        return new OkObjectResult(ApiResponse.Ok(items));
    }

    public async Task<IActionResult> GetByIdAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid approval item ID format."));

        var item = await reader.GetByIdAsync(intId);
        return item is null
            ? new NotFoundObjectResult(ApiResponse.Fail("Approval item not found."))
            : new OkObjectResult(ApiResponse.Ok(item));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<CreateApprovalItemRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.Code))
            return new BadRequestObjectResult(ApiResponse.Fail("Name and Code are required."));

        if (await db.ApprovalItems.AnyAsync(a => a.Code == body.Code))
            throw AppException.Conflict($"Approval item code '{body.Code}' already exists.");

        if (body.ApplicationType is not null &&
            await db.ApprovalItems.AnyAsync(a => a.ApplicationType == body.ApplicationType))
            throw AppException.Conflict($"An approval flow for application type '{body.ApplicationType}' already exists.");

        var item = new ApprovalItem
        {
            Name            = body.Name,
            Code            = body.Code,
            Description     = body.Description,
            IsActive        = body.IsActive,
            ApplicationType = body.ApplicationType,
            CreatedAt       = Clock.Now,
        };
        db.ApprovalItems.Add(item);
        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(item.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Approval item created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid approval item ID format."));

        var body = await req.ReadFromJsonAsync<UpdateApprovalItemRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var item = await db.ApprovalItems.FindAsync(intId)
            ?? throw AppException.NotFound("ApprovalItem");

        if (body.Name        is not null) item.Name        = body.Name;
        if (body.Code        is not null) item.Code        = body.Code;
        if (body.Description is not null) item.Description = body.Description;
        if (body.IsActive.HasValue)       item.IsActive    = body.IsActive.Value;

        if (body.ApplicationType is not null)
        {
            var conflictType = body.ApplicationType == "" ? null : body.ApplicationType;
            if (conflictType is not null &&
                await db.ApprovalItems.AnyAsync(a => a.ApplicationType == conflictType && a.Id != intId))
                throw AppException.Conflict($"An approval flow for application type '{conflictType}' already exists.");
            item.ApplicationType = conflictType;
        }

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(item.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Approval item updated."));
    }

    public async Task<IActionResult> DeleteAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid approval item ID format."));

        var item = await db.ApprovalItems.FindAsync(intId)
            ?? throw AppException.NotFound("ApprovalItem");

        db.ApprovalItems.Remove(item);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Approval item '{id}' deleted."));
    }

    // ── Approval Steps ────────────────────────────────────────────────────

    public async Task<IActionResult> AddStepAsync(HttpRequest req, string itemId)
    {
        if (!int.TryParse(itemId, out var intItemId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid approval item ID format."));

        var body = await req.ReadFromJsonAsync<CreateApprovalStepRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        // UseApplicantDepartment 時只需 JobTitleId；否則部門或職稱至少選一
        if (body.UseApplicantDepartment)
        {
            if (body.JobTitleId is null)
                return new BadRequestObjectResult(ApiResponse.Fail("JobTitleId is required when UseApplicantDepartment is enabled."));
        }
        else if (body.DepartmentId is null && body.JobTitleId is null)
        {
            return new BadRequestObjectResult(ApiResponse.Fail("At least one of DepartmentId or JobTitleId is required."));
        }

        if (!await db.ApprovalItems.AnyAsync(a => a.Id == intItemId))
            throw AppException.NotFound("ApprovalItem");

        var step = new ApprovalStep
        {
            ApprovalItemId         = intItemId,
            StepOrder              = body.StepOrder,
            DepartmentId           = body.UseApplicantDepartment ? null : body.DepartmentId,
            JobTitleId             = body.JobTitleId,
            UseApplicantDepartment = body.UseApplicantDepartment,
            Note                   = body.Note,
            CreatedAt              = Clock.Now,
        };
        db.ApprovalSteps.Add(step);
        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(intItemId);
        return new ObjectResult(ApiResponse.Ok(dto, "Step added.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateStepAsync(HttpRequest req, string itemId, string stepId)
    {
        if (!int.TryParse(itemId, out var intItemId) || !int.TryParse(stepId, out var intStepId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid ID format."));

        var body = await req.ReadFromJsonAsync<UpdateApprovalStepRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var step = await db.ApprovalSteps
            .FirstOrDefaultAsync(s => s.Id == intStepId && s.ApprovalItemId == intItemId)
            ?? throw AppException.NotFound("ApprovalStep");

        if (body.StepOrder.HasValue)              step.StepOrder              = body.StepOrder.Value;
        if (body.UseApplicantDepartment.HasValue) step.UseApplicantDepartment = body.UseApplicantDepartment.Value;
        if (body.DepartmentId.HasValue)           step.DepartmentId           = body.DepartmentId == 0 ? null : body.DepartmentId;
        if (body.JobTitleId.HasValue)             step.JobTitleId             = body.JobTitleId   == 0 ? null : body.JobTitleId;
        if (body.Note        is not null)         step.Note                   = body.Note;

        // UseApplicantDepartment 時清除固定部門、且 JobTitleId 必填
        if (step.UseApplicantDepartment)
        {
            step.DepartmentId = null;
            if (step.JobTitleId is null)
                return new BadRequestObjectResult(ApiResponse.Fail("JobTitleId is required when UseApplicantDepartment is enabled."));
        }
        else if (step.DepartmentId is null && step.JobTitleId is null)
        {
            return new BadRequestObjectResult(ApiResponse.Fail("At least one of DepartmentId or JobTitleId is required."));
        }

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(intItemId);
        return new OkObjectResult(ApiResponse.Ok(dto, "Step updated."));
    }

    public async Task<IActionResult> DeleteStepAsync(string itemId, string stepId)
    {
        if (!int.TryParse(itemId, out var intItemId) || !int.TryParse(stepId, out var intStepId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid ID format."));

        var step = await db.ApprovalSteps
            .FirstOrDefaultAsync(s => s.Id == intStepId && s.ApprovalItemId == intItemId)
            ?? throw AppException.NotFound("ApprovalStep");

        db.ApprovalSteps.Remove(step);
        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(intItemId);
        return new OkObjectResult(ApiResponse.Ok(dto, "Step deleted."));
    }
}
