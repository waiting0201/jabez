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
/// GET    /leave-requests                    → 列表
/// POST   /leave-requests                    → 新增（EmployeeId 由 JWT 決定）
/// GET    /leave-requests/compensatory-hours → 可補休時數查詢
/// GET    /leave-requests/{id}               → 單筆
/// PUT    /leave-requests/{id}               → 更新（僅 draft 才允許）
/// DELETE /leave-requests/{id}               → 刪除（僅 draft 才允許）
/// PATCH  /leave-requests/{id}/submit        → 送出（draft → pending）
/// </summary>
public sealed class LeaveRequestHandler(
    AppDbContext db,
    ILeaveRequestReadService reader,
    IJwtService jwtService,
    IApprovalNotificationService notifier,
    IApprovalFlowService approvalFlow)
{
    private static readonly HashSet<string> ValidLeaveTypes =
        ["annual", "personal", "sick", "compensatory", "marriage", "bereavement", "maternity", "paternity", "official"];
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        Guid? filterUserId = user?.IsSuperAdmin == true ? null : userId;
        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        var result = await reader.GetPagedAsync(page, pageSize, filterUserId);
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    public async Task<IActionResult> GetByIdAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid leave request ID format."));

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var exists = user?.IsSuperAdmin == true
            ? await db.LeaveRequests.AnyAsync(x => x.Id == intId)
            : await db.LeaveRequests.AnyAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (!exists)
            return new NotFoundObjectResult(ApiResponse.Fail("Leave request not found."));

        var item = await reader.GetByIdAsync(intId);
        return new OkObjectResult(ApiResponse.Ok(item));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        // BUG-04: EmployeeId 由 JWT 中的 sub claim 決定，不信任客戶端傳入的值
        var employeeId = await GetUserIdAsync(req);

        var body = await req.ReadFromJsonAsync<CreateLeaveRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (string.IsNullOrWhiteSpace(body.LeaveType))
            return new BadRequestObjectResult(ApiResponse.Fail("LeaveType is required."));

        if (!ValidLeaveTypes.Contains(body.LeaveType))
            return new BadRequestObjectResult(ApiResponse.Fail(
                $"Invalid LeaveType '{body.LeaveType}'. Must be one of: {string.Join(", ", ValidLeaveTypes)}"));

        if (body.StartDate == default || body.EndDate == default)
            return new BadRequestObjectResult(ApiResponse.Fail("StartDate and EndDate are required."));

        if (body.EndDate <= body.StartDate)
            return new BadRequestObjectResult(ApiResponse.Fail("EndDate must be after StartDate."));

        // 分鐘必須為 00 或 30
        if (body.StartDate.Minute % 30 != 0)
            return new BadRequestObjectResult(ApiResponse.Fail("StartDate 的分鐘必須為 00 或 30。"));
        if (body.EndDate.Minute % 30 != 0)
            return new BadRequestObjectResult(ApiResponse.Fail("EndDate 的分鐘必須為 00 或 30。"));

        // 時數由開始/結束時間計算，不信任客戶端傳入值
        var hours = (decimal)(body.EndDate - body.StartDate).TotalHours;

        // 指定審核者存在性驗證
        if (body.DesignatedReviewerId.HasValue)
        {
            var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == body.DesignatedReviewerId.Value);
            if (!exists)
                return new BadRequestObjectResult(ApiResponse.Fail("指定的審核者不存在。"));
        }

        var item = new LeaveRequest
        {
            EmployeeId           = employeeId,   // 強制使用 JWT 身分，忽略 body.EmployeeId
            ApprovalItemId       = body.ApprovalItemId,
            LeaveType            = body.LeaveType,
            StartDate            = body.StartDate,
            EndDate              = body.EndDate,
            Hours                = hours,
            Reason               = body.Reason,
            ApprovalStatus       = "draft",
            DesignatedReviewerId = body.DesignatedReviewerId,
            CreatedAt            = Clock.Now,
        };
        db.LeaveRequests.Add(item);
        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(item.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Leave request created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid leave request ID format."));

        var body = await req.ReadFromJsonAsync<UpdateLeaveRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var item = await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId)
            ?? throw AppException.NotFound("LeaveRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned leave requests can be edited.");

        // 指定審核者存在性驗證（提供非空 Guid 時才驗證）
        if (body.DesignatedReviewerId.HasValue && body.DesignatedReviewerId != Guid.Empty)
        {
            var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == body.DesignatedReviewerId.Value);
            if (!exists)
                return new BadRequestObjectResult(ApiResponse.Fail("指定的審核者不存在。"));
        }

        if (body.LeaveType  is not null)            item.LeaveType           = body.LeaveType;
        if (body.StartDate.HasValue)                item.StartDate           = body.StartDate.Value;
        if (body.EndDate.HasValue)                  item.EndDate             = body.EndDate.Value;
        if (body.Reason    is not null)             item.Reason              = body.Reason;
        if (body.DesignatedReviewerId.HasValue)     item.DesignatedReviewerId = body.DesignatedReviewerId == Guid.Empty ? null : body.DesignatedReviewerId;

        // 時數由開始/結束時間重新計算
        item.Hours = (decimal)(item.EndDate - item.StartDate).TotalHours;

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(item.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Leave request updated."));
    }

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid leave request ID format."));

        var item = await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId)
            ?? throw AppException.NotFound("LeaveRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned leave requests can be deleted.");

        db.LeaveRequests.Remove(item);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Leave request '{id}' deleted."));
    }

    /// <summary>查詢當前使用者的可補休時數（總加班時數 - 已補休時數）</summary>
    public async Task<IActionResult> GetCompensatoryHoursAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);

        // 總加班時數：已核准的加班申請 EstimatedHours 合計
        var totalOvertimeHours = await db.OvertimeRequests
            .Where(o => o.EmployeeId == userId && o.ApprovalStatus == "approved")
            .SumAsync(o => o.EstimatedHours);

        // 已補休時數：已送出（pending / approved）的補休假 Hours 合計
        var usedCompensatoryHours = await db.LeaveRequests
            .Where(l => l.EmployeeId == userId
                     && l.LeaveType == "compensatory"
                     && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending"))
            .SumAsync(l => l.Hours);

        var available = totalOvertimeHours - usedCompensatoryHours;

        return new OkObjectResult(ApiResponse.Ok(new
        {
            totalOvertimeHours,
            usedCompensatoryHours,
            availableHours = available < 0 ? 0 : available,
        }));
    }

    /// <summary>計算指定使用者可用的補休時數</summary>
    private async Task<decimal> GetAvailableCompensatoryHoursAsync(Guid userId)
    {
        var totalOvertimeHours = await db.OvertimeRequests
            .Where(o => o.EmployeeId == userId && o.ApprovalStatus == "approved")
            .SumAsync(o => o.EstimatedHours);

        var usedCompensatoryHours = await db.LeaveRequests
            .Where(l => l.EmployeeId == userId
                     && l.LeaveType == "compensatory"
                     && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending"))
            .SumAsync(l => l.Hours);

        var available = totalOvertimeHours - usedCompensatoryHours;
        return available < 0 ? 0 : available;
    }

    /// <summary>送出申請（draft → pending）</summary>
    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid leave request ID format."));

        var item = await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId)
            ?? throw AppException.NotFound("LeaveRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned leave requests can be submitted.");

        // 退回重送時清除舊審核記錄，重新走流程
        if (item.ApprovalStatus == "returned")
        {
            var oldRecords = await db.ApprovalRecords
                .Where(r => r.ApplicationType == "leave" && r.ApplicationId == item.Id)
                .ToListAsync();
            db.ApprovalRecords.RemoveRange(oldRecords);

            var oldOverrides = await db.EscalationOverrides
                .Where(o => o.ApplicationType == "leave" && o.ApplicationId == item.Id)
                .ToListAsync();
            db.EscalationOverrides.RemoveRange(oldOverrides);
        }

        // 補休時數驗證：申請時數不得超過可用時數
        if (item.LeaveType == "compensatory")
        {
            var available = await GetAvailableCompensatoryHoursAsync(userId);
            var requestedHours = item.Hours;
            if (requestedHours > available)
                return new BadRequestObjectResult(ApiResponse.Fail(
                    $"補休時數不足。申請 {requestedHours} 小時，可用 {available} 小時。"));
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
            return new OkObjectResult(ApiResponse.Ok(saDto, "Leave request auto-approved."));
        }

        // 自動關聯簽核流程（依 ApplicationType 查找啟用的流程）
        if (item.ApprovalItemId is null)
        {
            var flow = await db.ApprovalItems
                .AsNoTracking()
                .FirstOrDefaultAsync(ai => ai.ApplicationType == "leave" && ai.IsActive);
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
            await approvalFlow.ResolveStartingStepAsync(item.ApprovalItemId, userId, "leave", item.DesignatedReviewerId);

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
                ApplicationType  = "leave",
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
                await notifier.NotifySpecificReviewerAsync("leave", item.Id, escalation.ReviewerId, userId, escalation.OnBehalfOfUserId is not null);
            else
            {
                // 檢查當前步驟是否為指定審核步驟
                bool isDesignatedStep = item.ApprovalItemId.HasValue && await db.ApprovalSteps.AsNoTracking()
                    .AnyAsync(s => s.ApprovalItemId == item.ApprovalItemId
                        && s.StepOrder == startStep
                        && s.UseApplicantDesignated);
                if (isDesignatedStep && item.DesignatedReviewerId.HasValue)
                    await notifier.NotifySpecificReviewerAsync("leave", item.Id, item.DesignatedReviewerId.Value, userId, false);
                else
                    await notifier.NotifyReviewersAsync("leave", item.Id, item.ApprovalItemId, startStep, userId);
            }
        }

        var dto = await reader.GetByIdAsync(item.Id);
        var msg = autoApproved ? "Leave request auto-approved." : "Leave request submitted.";
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
