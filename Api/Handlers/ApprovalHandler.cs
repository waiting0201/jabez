using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace Jabez.Api.Handlers;

public sealed class ApprovalHandler(AppDbContext db, IApprovalReadService reader, IJwtService jwtService)
{
    // ── Approval Items ───────────────────────────────────────────────────

    public async Task<IActionResult> GetAllAsync()
    {
        var items = await reader.GetAllAsync();
        return new OkObjectResult(ApiResponse.Ok(items));
    }

    /// <summary>
    /// 輕量級查詢：取得指定 ApplicationType 啟用中流程的精簡資訊。
    /// 不需 approvals:read 權限（登入即可），供申請表單判斷是否需顯示「指定審核者」欄位。
    /// </summary>
    public async Task<IActionResult> GetActiveByTypeAsync(HttpRequest req)
    {
        var type = req.Query["type"].ToString();
        if (string.IsNullOrWhiteSpace(type))
            return new BadRequestObjectResult(ApiResponse.Fail("Query parameter 'type' is required."));

        // 依呼叫者部門解析「實際會走的流程」：部門專屬優先，否則退回通用預設
        // 另帶呼叫者 UserId，讓回傳的 UseApplicantDesignated 反映例外指定審核名單是否命中呼叫者
        var principal = await jwtService.ValidateRequestAsync(req);
        int? departmentId = int.TryParse(principal?.FindFirst("department_id")?.Value, out var deptId) ? deptId : null;
        Guid? userId = Guid.TryParse(principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var uid) ? uid : null;

        var flow = await reader.GetActiveByTypeAsync(type, departmentId, userId);
        return new OkObjectResult(ApiResponse.Ok(flow));
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
            await db.ApprovalItems.AnyAsync(a => a.ApplicationType == body.ApplicationType && a.DepartmentId == body.DepartmentId))
            throw AppException.Conflict($"An approval flow for application type '{body.ApplicationType}' (department {body.DepartmentId?.ToString() ?? "預設"}) already exists.");

        var item = new ApprovalItem
        {
            Name            = body.Name,
            Code            = body.Code,
            Description     = body.Description,
            IsActive        = body.IsActive,
            ApplicationType = body.ApplicationType,
            DepartmentId    = body.DepartmentId,
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
        if (body.Code is not null)
        {
            if (body.Code != item.Code && await db.ApprovalItems.AnyAsync(a => a.Code == body.Code && a.Id != intId))
                throw AppException.Conflict($"Approval item code '{body.Code}' already exists.");
            item.Code = body.Code;
        }
        if (body.Description is not null) item.Description = body.Description;
        if (body.IsActive.HasValue)       item.IsActive    = body.IsActive.Value;

        if (body.ApplicationType is not null)
        {
            // ApplicationType 與 DepartmentId 同屬「流程身分」，一併更新並以 (類型, 部門) 組合判重。
            // 編輯表單一律帶完整值；DepartmentId 為 null 代表通用預設流程。
            var conflictType = body.ApplicationType == "" ? null : body.ApplicationType;
            if (conflictType is not null &&
                await db.ApprovalItems.AnyAsync(a => a.ApplicationType == conflictType && a.DepartmentId == body.DepartmentId && a.Id != intId))
                throw AppException.Conflict($"An approval flow for application type '{conflictType}' (department {body.DepartmentId?.ToString() ?? "預設"}) already exists.");
            item.ApplicationType = conflictType;
            item.DepartmentId    = body.DepartmentId;
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

    /// <summary>
    /// 整批替換某步驟的例外指定審核名單（null＝不動、[]＝清空）。
    /// 名單非空即代表啟用例外，不另設 bool 旗標；與 UseApplicantDesignated 互斥。
    /// 呼叫端負責 SaveChanges。回傳替換後的名單人數。
    /// </summary>
    private async Task<int> ApplyStepExceptionsAsync(ApprovalStep step, Guid[]? exceptionUserIds)
    {
        var existing = await db.ApprovalStepExceptions
            .Where(e => e.ApprovalStepId == step.Id)
            .ToListAsync();

        // 原生指定審核步驟不需要例外名單 → 一律清空（切換模式時避免殘留孤兒設定）
        if (step.UseApplicantDesignated)
        {
            if (exceptionUserIds is { Length: > 0 })
                throw AppException.BadRequest("此步驟已設為「申請人指定審核」，無需再設定例外名單。");
            if (existing.Count > 0)
                db.ApprovalStepExceptions.RemoveRange(existing);
            return 0;
        }

        if (exceptionUserIds is null)
            return existing.Count; // 不動

        var ids = exceptionUserIds.Distinct().ToList();

        if (ids.Count > 0)
        {
            var validCount = await db.Users.CountAsync(u => ids.Contains(u.Id) && !u.IsSuperAdmin);
            if (validCount != ids.Count)
                throw AppException.BadRequest("例外名單中包含不存在或不可指定的使用者。");
        }

        db.ApprovalStepExceptions.RemoveRange(existing.Where(e => !ids.Contains(e.UserId)));
        foreach (var id in ids.Where(i => !existing.Any(e => e.UserId == i)))
            db.ApprovalStepExceptions.Add(new ApprovalStepException
            {
                ApprovalStepId = step.Id,
                UserId         = id,
                CreatedAt      = Clock.Now,
            });

        return ids.Count;
    }

    public async Task<IActionResult> AddStepAsync(HttpRequest req, string itemId)
    {
        if (!int.TryParse(itemId, out var intItemId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid approval item ID format."));

        var body = await req.ReadFromJsonAsync<CreateApprovalStepRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        bool hasExceptions = body.ExceptionUserIds is { Length: > 0 };

        // 例外指定審核與原生「申請人指定審核」互斥
        if (body.UseApplicantDesignated && hasExceptions)
            return new BadRequestObjectResult(ApiResponse.Fail("此步驟已設為「申請人指定審核」，無需再設定例外名單。"));

        // 三種模式互斥：UseApplicantDesignated / UseDirectSupervisor / 一般模式
        if (body.UseApplicantDesignated)
        {
            // 申請人指定審核模式：不需要 DepartmentId / JobTitleId / UseApplicantDepartment
        }
        else if (body.UseDirectSupervisor)
        {
            // 上層級模式：自動使用申請人部門，不需指定職稱
        }
        else if (body.UseApplicantDepartment)
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
            ApprovalItemId          = intItemId,
            StepOrder               = body.StepOrder,
            // UseApplicantDesignated 時清除部門/職稱限制
            DepartmentId            = (body.UseApplicantDesignated || body.UseDirectSupervisor || body.UseApplicantDepartment) ? null : body.DepartmentId,
            JobTitleId              = (body.UseApplicantDesignated || body.UseDirectSupervisor) ? null : body.JobTitleId,
            UseApplicantDepartment  = !body.UseApplicantDesignated && (body.UseDirectSupervisor || body.UseApplicantDepartment),
            UseDirectSupervisor     = !body.UseApplicantDesignated && body.UseDirectSupervisor,
            UseApplicantDesignated  = body.UseApplicantDesignated,
            // 指定審核（原生或例外）時此旗標才有意義（此步驟需先選部門再選人）
            DesignatedRequiresDepartment = (body.UseApplicantDesignated || hasExceptions) && body.DesignatedRequiresDepartment,
            MinDays                 = body.MinDays is > 0 ? body.MinDays : null,
            Note                    = body.Note,
            CreatedAt               = Clock.Now,
        };
        db.ApprovalSteps.Add(step);
        await db.SaveChangesAsync(); // 先存以取得 step.Id，例外名單需以其為 FK

        await ApplyStepExceptionsAsync(step, body.ExceptionUserIds);
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

        if (body.StepOrder.HasValue)                 step.StepOrder               = body.StepOrder.Value;
        if (body.UseApplicantDesignated.HasValue)    step.UseApplicantDesignated  = body.UseApplicantDesignated.Value;
        if (body.UseApplicantDepartment.HasValue)    step.UseApplicantDepartment  = body.UseApplicantDepartment.Value;
        if (body.UseDirectSupervisor.HasValue)       step.UseDirectSupervisor     = body.UseDirectSupervisor.Value;
        if (body.DepartmentId.HasValue)              step.DepartmentId            = body.DepartmentId == 0 ? null : body.DepartmentId;
        if (body.JobTitleId.HasValue)                step.JobTitleId              = body.JobTitleId   == 0 ? null : body.JobTitleId;
        if (body.Note        is not null)            step.Note                    = body.Note;
        if (body.DesignatedRequiresDepartment.HasValue) step.DesignatedRequiresDepartment = body.DesignatedRequiresDepartment.Value;
        // MinDays：編輯表單一律帶完整值，直接覆寫（<=0 或未填視為無門檻 null）
        step.MinDays = body.MinDays is > 0 ? body.MinDays : null;

        // 三種模式互斥，以最後設定的模式為準
        if (step.UseApplicantDesignated)
        {
            // 申請人指定審核模式：清除所有部門/職稱限制
            step.DepartmentId           = null;
            step.JobTitleId             = null;
            step.UseApplicantDepartment = false;
            step.UseDirectSupervisor    = false;
        }
        else if (step.UseDirectSupervisor)
        {
            // 直接上司模式：清除固定部門與職稱，自動視為使用申請人部門
            step.DepartmentId           = null;
            step.JobTitleId             = null;
            step.UseApplicantDepartment = true;
        }
        else
        {
            // 從其他模式切回一般模式時，重設相關旗標（除非呼叫者明確指定）
            if (body.UseDirectSupervisor == false && !body.UseApplicantDepartment.HasValue)
                step.UseApplicantDepartment = false;
            if (body.UseApplicantDesignated == false)
                step.UseApplicantDesignated = false;
        }

        // 例外指定審核名單（整批替換；切成 UseApplicantDesignated 時會自動清空）
        int exceptionCount = await ApplyStepExceptionsAsync(step, body.ExceptionUserIds);

        // DesignatedRequiresDepartment 僅在指定審核模式（原生或例外）下有意義
        if (!step.UseApplicantDesignated && exceptionCount == 0)
            step.DesignatedRequiresDepartment = false;

        if (!step.UseApplicantDesignated && !step.UseDirectSupervisor && step.UseApplicantDepartment)
        {
            step.DepartmentId = null;
            if (step.JobTitleId is null)
                return new BadRequestObjectResult(ApiResponse.Fail("JobTitleId is required when UseApplicantDepartment is enabled."));
        }
        else if (!step.UseApplicantDesignated && step.DepartmentId is null && step.JobTitleId is null && !step.UseDirectSupervisor && !step.UseApplicantDepartment)
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
