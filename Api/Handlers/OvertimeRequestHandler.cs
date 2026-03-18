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

/// <summary>
/// GET    /overtime-requests           → 列表（支援 ?status=&amp;date=YYYY-MM-DD 篩選，回傳當前使用者資料）
/// POST   /overtime-requests           → 新增（EmployeeId 由 JWT 決定）
/// GET    /overtime-requests/{id}      → 單筆
/// PUT    /overtime-requests/{id}      → 更新（僅 draft 才允許）
/// DELETE /overtime-requests/{id}      → 刪除（僅 draft 才允許）
/// PATCH  /overtime-requests/{id}/submit → 送出（draft → pending）
/// </summary>
public sealed class OvertimeRequestHandler(
    AppDbContext db,
    IOvertimeRequestReadService reader,
    IJwtService jwtService,
    IApprovalNotificationService notifier,
    IApprovalFlowService approvalFlow)
{
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var statusParam = req.Query["status"].ToString();
        var dateParam   = req.Query["date"].ToString();

        // 若有 status 或 date 查詢參數，走篩選路徑並以當前使用者身分過濾
        bool hasFilter = !string.IsNullOrEmpty(statusParam) || !string.IsNullOrEmpty(dateParam);
        if (hasFilter)
        {
            string?   status = string.IsNullOrEmpty(statusParam) ? null : statusParam;
            DateOnly? date   = DateOnly.TryParse(dateParam, out var d) ? d : (DateOnly?)null;

            var filtered = await reader.GetFilteredAsync(status, date, userId);
            return new OkObjectResult(ApiResponse.Ok(filtered));
        }

        // 預設分頁列表
        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        var result = await reader.GetPagedAsync(page, pageSize, userId);
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    public async Task<IActionResult> GetByIdAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid overtime request ID format."));

        if (!await db.OvertimeRequests.AnyAsync(x => x.Id == intId && x.EmployeeId == userId))
            return new NotFoundObjectResult(ApiResponse.Fail("Overtime request not found."));

        var item = await reader.GetByIdAsync(intId);
        return new OkObjectResult(ApiResponse.Ok(item));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        // BUG-04: EmployeeId 由 JWT 中的 sub claim 決定，不信任客戶端傳入的值
        var employeeId = await GetUserIdAsync(req);

        var body = await req.ReadFromJsonAsync<CreateOvertimeRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (body.OvertimeDate == default)
            return new BadRequestObjectResult(ApiResponse.Fail("OvertimeDate is required."));

        if (string.IsNullOrWhiteSpace(body.Reason))
            return new BadRequestObjectResult(ApiResponse.Fail("Reason is required."));

        if (body.EstimatedHours <= 0)
            return new BadRequestObjectResult(ApiResponse.Fail("EstimatedHours must be greater than 0."));

        // 指定審核者存在性驗證
        if (body.DesignatedReviewerId.HasValue)
        {
            var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == body.DesignatedReviewerId.Value);
            if (!exists)
                return new BadRequestObjectResult(ApiResponse.Fail("指定的審核者不存在。"));
        }

        var item = new OvertimeRequest
        {
            EmployeeId           = employeeId,   // 強制使用 JWT 身分，忽略 body.EmployeeId
            ApprovalItemId       = body.ApprovalItemId,
            OvertimeDate         = body.OvertimeDate,
            ProjectIds           = body.ProjectIds is { Length: > 0 } ? string.Join(",", body.ProjectIds) : null,
            EstimatedHours       = body.EstimatedHours,
            Reason               = body.Reason,
            ApprovalStatus       = "draft",
            DesignatedReviewerId = body.DesignatedReviewerId,
            CreatedAt            = Clock.Now,
        };
        db.OvertimeRequests.Add(item);
        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(item.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Overtime request created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid overtime request ID format."));

        var body = await req.ReadFromJsonAsync<UpdateOvertimeRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var item = await db.OvertimeRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId)
            ?? throw AppException.NotFound("OvertimeRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned overtime requests can be edited.");

        // 指定審核者存在性驗證（提供非空 Guid 時才驗證）
        if (body.DesignatedReviewerId.HasValue && body.DesignatedReviewerId != Guid.Empty)
        {
            var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == body.DesignatedReviewerId.Value);
            if (!exists)
                return new BadRequestObjectResult(ApiResponse.Fail("指定的審核者不存在。"));
        }

        if (body.OvertimeDate.HasValue)             item.OvertimeDate        = body.OvertimeDate.Value;
        if (body.ProjectIds is not null)            item.ProjectIds          = body.ProjectIds.Length > 0 ? string.Join(",", body.ProjectIds) : null;
        if (body.EstimatedHours.HasValue)           item.EstimatedHours      = body.EstimatedHours.Value;
        if (body.Reason is not null)                item.Reason              = body.Reason;
        if (body.DesignatedReviewerId.HasValue)     item.DesignatedReviewerId = body.DesignatedReviewerId == Guid.Empty ? null : body.DesignatedReviewerId;

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(item.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Overtime request updated."));
    }

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid overtime request ID format."));

        var item = await db.OvertimeRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId)
            ?? throw AppException.NotFound("OvertimeRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned overtime requests can be deleted.");

        db.OvertimeRequests.Remove(item);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Overtime request '{id}' deleted."));
    }

    /// <summary>送出申請（draft → pending）</summary>
    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid overtime request ID format."));

        var item = await db.OvertimeRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId)
            ?? throw AppException.NotFound("OvertimeRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned overtime requests can be submitted.");

        // 退回重送時清除舊審核記錄，重新走流程
        if (item.ApprovalStatus == "returned")
        {
            var oldRecords = await db.ApprovalRecords
                .Where(r => r.ApplicationType == "overtime" && r.ApplicationId == item.Id)
                .ToListAsync();
            db.ApprovalRecords.RemoveRange(oldRecords);

            var oldOverrides = await db.EscalationOverrides
                .Where(o => o.ApplicationType == "overtime" && o.ApplicationId == item.Id)
                .ToListAsync();
            db.EscalationOverrides.RemoveRange(oldOverrides);
        }

        // Superadmin 無部門歸屬，直接自動核准
        var submitter = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (submitter?.IsSuperAdmin == true)
        {
            item.ApprovalStatus   = "approved";
            item.CurrentStepOrder = 1;
            item.ReviewedAt       = Clock.Now;
            item.ReviewedById     = userId;
            item.ReviewNote       = "系統自動核准（Superadmin）";
            await db.SaveChangesAsync();
            var saDto = await reader.GetByIdAsync(item.Id);
            return new OkObjectResult(ApiResponse.Ok(saDto, "Overtime request auto-approved."));
        }

        // 自動關聯簽核流程（依 ApplicationType 查找啟用的流程）
        if (item.ApprovalItemId is null)
        {
            var flow = await db.ApprovalItems
                .AsNoTracking()
                .FirstOrDefaultAsync(ai => ai.ApplicationType == "overtime" && ai.IsActive);
            if (flow is not null)
                item.ApprovalItemId = flow.Id;
        }

        // 若流程中有 UseApplicantDesignated 步驟，DesignatedReviewerId 必填
        if (item.ApprovalItemId.HasValue)
        {
            bool hasDesignatedStep = await db.ApprovalSteps.AsNoTracking()
                .AnyAsync(s => s.ApprovalItemId == item.ApprovalItemId && s.UseApplicantDesignated);
            if (hasDesignatedStep && !item.DesignatedReviewerId.HasValue)
                return new BadRequestObjectResult(ApiResponse.Fail("此簽核流程包含申請人指定審核步驟，請提供 DesignatedReviewerId。"));
        }

        // 解析審核步驟（含升級審核邏輯）
        var (startStep, autoApproved, escalation) =
            await approvalFlow.ResolveStartingStepAsync(item.ApprovalItemId, userId, "overtime", item.DesignatedReviewerId);

        if (autoApproved)
        {
            item.ApprovalStatus   = "approved";
            item.CurrentStepOrder = startStep;
            item.ReviewedAt       = Clock.Now;
            item.ReviewedById     = userId;
            item.ReviewNote       = "系統自動核准（所有審核步驟皆為申請人本人）";
        }
        else
        {
            item.ApprovalStatus   = "pending";
            item.CurrentStepOrder = startStep;
        }

        // 升級審核：記錄指派的審核者
        if (escalation is not null)
        {
            db.EscalationOverrides.Add(new EscalationOverride
            {
                ApplicationType  = "overtime",
                ApplicationId    = item.Id,
                StepOrder        = startStep,
                ReviewerId       = escalation.ReviewerId,
                OnBehalfOfUserId = escalation.OnBehalfOfUserId,
                CreatedAt        = Clock.Now,
            });
        }

        await db.SaveChangesAsync();

        // 通知審核者
        if (!autoApproved)
        {
            if (escalation is not null)
                await notifier.NotifySpecificReviewerAsync("overtime", item.Id, escalation.ReviewerId, userId, escalation.OnBehalfOfUserId is not null);
            else
            {
                bool isDesignatedStep = item.ApprovalItemId.HasValue && await db.ApprovalSteps.AsNoTracking()
                    .AnyAsync(s => s.ApprovalItemId == item.ApprovalItemId
                        && s.StepOrder == startStep
                        && s.UseApplicantDesignated);
                if (isDesignatedStep && item.DesignatedReviewerId.HasValue)
                    await notifier.NotifySpecificReviewerAsync("overtime", item.Id, item.DesignatedReviewerId.Value, userId, false);
                else
                    await notifier.NotifyReviewersAsync("overtime", item.Id, item.ApprovalItemId, startStep, userId);
            }
        }

        var dto = await reader.GetByIdAsync(item.Id);
        var msg = autoApproved ? "Overtime request auto-approved." : "Overtime request submitted.";
        return new OkObjectResult(ApiResponse.Ok(dto, msg));
    }

    // ── Helper ──────────────────────────────────────────────────────────────────

    /// <summary>從 JWT Bearer Token 取出 sub claim 作為使用者 GUID</summary>
    private async Task<Guid> GetUserIdAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw AppException.Unauthorized("Invalid token claims.");
        return userId;
    }
}
