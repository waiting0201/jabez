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
/// 簽核作業：彙總 PaymentRequests、LeaveRequests、TravelRequests、OvertimeRequests 的統一審核視圖。
/// GET   /approval-tasks                                    → 全部申請（依審核者職稱過濾）
/// GET   /approval-tasks/{id}                               → 單筆
/// PATCH /approval-tasks/{applicationType}/{id}/review      → 多步驟審核（核准 / 退回修改 / 拒絕）
/// </summary>
public sealed class ApprovalTaskHandler(AppDbContext db, IPaymentRequestReadService reader, IJwtService jwtService, IApprovalNotificationService notifier)
{
    private static readonly HashSet<string> ValidActions  = ["approved", "returned", "rejected"];
    public  static readonly HashSet<string> ValidAppTypes = ["payment_request", "leave", "travel", "overtime"];

    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        // 依審核者職稱過濾，讓每個人只看到「當前步驟符合自己職稱」的待審申請
        // status 參數：pending（待審）、approved（已核准），空值沿用既有行為
        var principal = await jwtService.ValidateRequestAsync(req);
        int?  jobTitleId    = null;
        int?  deptId        = null;
        Guid? reviewerUserId = null;

        if (principal is not null)
        {
            var userIdStr = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (Guid.TryParse(userIdStr, out var userId))
            {
                reviewerUserId = userId;
                var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (user is not null && !user.IsSuperAdmin)
                {
                    jobTitleId = user.JobTitleId;
                    deptId     = user.DepartmentId;
                }
            }
        }

        int    page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int    pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        string? status  = req.Query["status"].ToString() is { Length: > 0 } s ? s : null;

        var allTasks = (await reader.GetApprovalTasksAsync(jobTitleId, deptId, status, reviewerUserId)).ToList();
        int total = allTasks.Count;
        var items = allTasks.Skip((page - 1) * pageSize).Take(pageSize);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        var result = new PagedResult<ApprovalTaskDto>(items, total, page, pageSize, Math.Max(1, totalPages));
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    public async Task<IActionResult> GetByIdAsync(string id, string? applicationType = null)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid approval task ID format."));

        var task = !string.IsNullOrEmpty(applicationType)
            ? await reader.GetApprovalTaskByIdAsync(intId, applicationType)
            : await reader.GetApprovalTaskByIdAsync(intId);
        return task is null
            ? new NotFoundObjectResult(ApiResponse.Fail("Approval task not found.", $"No request with id '{id}'."))
            : new OkObjectResult(ApiResponse.Ok(task));
    }

    public async Task<IActionResult> ReviewAsync(HttpRequest req, string applicationType, string id)
    {
        if (!ValidAppTypes.Contains(applicationType))
            return new BadRequestObjectResult(
                ApiResponse.Fail($"Invalid application type '{applicationType}'."));

        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid approval task ID format."));

        var body = await req.ReadFromJsonAsync<ReviewPaymentRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (!ValidActions.Contains(body.Action))
            return new BadRequestObjectResult(
                ApiResponse.Fail($"Invalid action '{body.Action}'. Must be 'approved', 'returned', or 'rejected'."));

        if ((body.Action == "rejected" || body.Action == "returned") && string.IsNullOrWhiteSpace(body.ReviewNote))
            return new BadRequestObjectResult(
                ApiResponse.Fail("ReviewNote is required when returning or rejecting a request."));

        // ── 取得目前審核者 ──────────────────────────────────────────────────
        var principal = await jwtService.ValidateRequestAsync(req);
        if (principal is null)
            return new UnauthorizedObjectResult(ApiResponse.Fail("Unauthorized."));

        var userIdStr = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var reviewerId))
            return new UnauthorizedObjectResult(ApiResponse.Fail("Invalid token claims."));

        var reviewer = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == reviewerId);
        if (reviewer is null)
            return new UnauthorizedObjectResult(ApiResponse.Fail("Reviewer not found."));

        // ── 依申請類型處理 ─────────────────────────────────────────────────
        switch (applicationType)
        {
            case "payment_request":
            {
                var pr = await db.PaymentRequests.FindAsync(intId)
                    ?? throw AppException.NotFound("PaymentRequest");
                if (pr.ApprovalStatus != "pending")
                    throw AppException.BadRequest("Only pending payment requests can be reviewed.");

                var prApplicant = pr.SubmittedById.HasValue
                    ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == pr.SubmittedById.Value)
                    : null;
                await AuthorizeStepAsync(pr.ApprovalItemId, pr.CurrentStepOrder, reviewer, prApplicant?.DepartmentId, "payment_request", pr.Id);
                // 設定預計撥款日 / 撥款日（審核者可在審核時填寫）
                if (body.EstimatedPaymentDate.HasValue)
                    pr.EstimatedPaymentDate = body.EstimatedPaymentDate.Value;
                if (body.PaidAt.HasValue)
                    pr.PaidAt = body.PaidAt.Value;
                await ProcessReviewAsync("payment_request", pr.Id, pr.CurrentStepOrder,
                    pr.ApprovalItemId, body.Action, body.ReviewNote, reviewerId, pr.SubmittedById,
                    setStatus:    s  => pr.ApprovalStatus   = s,
                    incrementStep:    () => pr.CurrentStepOrder++,
                    setReviewed:      () => { pr.ReviewedAt = Clock.Now; pr.ReviewedById = reviewerId; pr.ReviewNote = body.ReviewNote?.Trim(); });
                await db.SaveChangesAsync();
                break;
            }
            case "leave":
            {
                var lr = await db.LeaveRequests.FindAsync(intId)
                    ?? throw AppException.NotFound("LeaveRequest");
                if (lr.ApprovalStatus != "pending")
                    throw AppException.BadRequest("Only pending leave requests can be reviewed.");

                var lrApplicant = lr.EmployeeId.HasValue
                    ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == lr.EmployeeId.Value)
                    : null;
                await AuthorizeStepAsync(lr.ApprovalItemId, lr.CurrentStepOrder, reviewer, lrApplicant?.DepartmentId, "leave", lr.Id);
                await ProcessReviewAsync("leave", lr.Id, lr.CurrentStepOrder,
                    lr.ApprovalItemId, body.Action, body.ReviewNote, reviewerId, lr.EmployeeId,
                    setStatus:    s  => lr.ApprovalStatus   = s,
                    incrementStep:    () => lr.CurrentStepOrder++,
                    setReviewed:      () => { lr.ReviewedAt = Clock.Now; lr.ReviewedById = reviewerId; lr.ReviewNote = body.ReviewNote?.Trim(); });
                await db.SaveChangesAsync();
                break;
            }
            case "travel":
            {
                var tr = await db.TravelRequests.FindAsync(intId)
                    ?? throw AppException.NotFound("TravelRequest");
                if (tr.ApprovalStatus != "pending")
                    throw AppException.BadRequest("Only pending travel requests can be reviewed.");

                var trApplicant = tr.EmployeeId.HasValue
                    ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == tr.EmployeeId.Value)
                    : null;
                await AuthorizeStepAsync(tr.ApprovalItemId, tr.CurrentStepOrder, reviewer, trApplicant?.DepartmentId, "travel", tr.Id);
                await ProcessReviewAsync("travel", tr.Id, tr.CurrentStepOrder,
                    tr.ApprovalItemId, body.Action, body.ReviewNote, reviewerId, tr.EmployeeId,
                    setStatus:    s  => tr.ApprovalStatus   = s,
                    incrementStep:    () => tr.CurrentStepOrder++,
                    setReviewed:      () => { tr.ReviewedAt = Clock.Now; tr.ReviewedById = reviewerId; tr.ReviewNote = body.ReviewNote?.Trim(); });
                await db.SaveChangesAsync();
                break;
            }
            case "overtime":
            {
                var ot = await db.OvertimeRequests.FindAsync(intId)
                    ?? throw AppException.NotFound("OvertimeRequest");
                if (ot.ApprovalStatus != "pending")
                    throw AppException.BadRequest("Only pending overtime requests can be reviewed.");

                var otApplicant = ot.EmployeeId.HasValue
                    ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == ot.EmployeeId.Value)
                    : null;
                await AuthorizeStepAsync(ot.ApprovalItemId, ot.CurrentStepOrder, reviewer, otApplicant?.DepartmentId, "overtime", ot.Id);
                await ProcessReviewAsync("overtime", ot.Id, ot.CurrentStepOrder,
                    ot.ApprovalItemId, body.Action, body.ReviewNote, reviewerId, ot.EmployeeId,
                    setStatus:    s  => ot.ApprovalStatus   = s,
                    incrementStep:    () => ot.CurrentStepOrder++,
                    setReviewed:      () => { ot.ReviewedAt = Clock.Now; ot.ReviewedById = reviewerId; ot.ReviewNote = body.ReviewNote?.Trim(); });
                await db.SaveChangesAsync();
                break;
            }
            default:
                return new BadRequestObjectResult(ApiResponse.Fail("Unknown application type."));
        }

        var task = await reader.GetApprovalTaskByIdAsync(intId, applicationType);
        return new OkObjectResult(ApiResponse.Ok(task, $"Request {body.Action}."));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>驗證當前審核者是否有權審核此步驟（依職稱/部門設定，或升級審核指派）。Superadmin 可審核任何步驟。</summary>
    private async Task AuthorizeStepAsync(
        int? approvalItemId, int currentStepOrder, User reviewer,
        int? applicantDepartmentId = null, string? applicationType = null, int? applicationId = null)
    {
        if (approvalItemId is null) return;

        // Superadmin 可審核任何步驟
        if (reviewer.IsSuperAdmin) return;

        var step = await db.ApprovalSteps
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ApprovalItemId == approvalItemId && s.StepOrder == currentStepOrder);

        if (step is null) return;

        bool jobTitleOk = step.JobTitleId is null || step.JobTitleId == reviewer.JobTitleId;

        bool deptOk;
        if (step.UseApplicantDepartment)
        {
            deptOk = applicantDepartmentId is not null && applicantDepartmentId == reviewer.DepartmentId;
        }
        else
        {
            deptOk = step.DepartmentId is null || step.DepartmentId == reviewer.DepartmentId;
        }

        if (!jobTitleOk || !deptOk)
        {
            // 檢查是否為升級審核指派的審核者
            if (applicationType is not null && applicationId.HasValue)
            {
                var hasOverride = await db.EscalationOverrides
                    .AsNoTracking()
                    .AnyAsync(e =>
                        e.ApplicationType == applicationType
                        && e.ApplicationId == applicationId.Value
                        && e.StepOrder == currentStepOrder
                        && e.ReviewerId == reviewer.Id);
                if (hasOverride) return; // 升級審核者，授權通過
            }
            throw AppException.Forbidden("You are not authorized to review this step.");
        }
    }

    /// <summary>寫入審核紀錄並根據 action 更新申請狀態與步驟，完成後觸發通知。</summary>
    private async Task ProcessReviewAsync(
        string applicationType, int applicationId, int currentStepOrder,
        int? approvalItemId, string action, string? reviewNote, Guid reviewerId, Guid? applicantId,
        Action<string> setStatus, Action incrementStep, Action setReviewed)
    {
        // 查詢升級審核指派（若有）
        var escalation = await db.EscalationOverrides
            .FirstOrDefaultAsync(e => e.ApplicationType == applicationType
                && e.ApplicationId == applicationId
                && e.StepOrder == currentStepOrder);

        // 寫入每步審核紀錄（含代理標記）
        db.ApprovalRecords.Add(new ApprovalRecord
        {
            ApplicationType  = applicationType,
            ApplicationId    = applicationId,
            StepOrder        = currentStepOrder,
            Action           = action,
            ReviewedById     = reviewerId,
            ReviewedAt       = Clock.Now,
            ReviewNote       = reviewNote?.Trim(),
            OnBehalfOfUserId = escalation?.OnBehalfOfUserId,
            IsEscalated      = escalation is not null,
        });

        // 清除已使用的升級指派
        if (escalation is not null)
            db.EscalationOverrides.Remove(escalation);

        if (action == "approved")
        {
            int totalSteps = approvalItemId.HasValue
                ? await db.ApprovalSteps.CountAsync(s => s.ApprovalItemId == approvalItemId)
                : 0;

            if (totalSteps > 0 && currentStepOrder < totalSteps)
            {
                incrementStep(); // 進入下一步，status 維持 "pending"
                // 通知下一步審核者
                if (applicantId.HasValue)
                    await notifier.NotifyReviewersAsync(applicationType, applicationId,
                        approvalItemId, currentStepOrder + 1, applicantId.Value);
            }
            else
            {
                setStatus("approved"); // 所有步驟完成
                setReviewed();
                // 通知申請人：已核准
                if (applicantId.HasValue)
                    await notifier.NotifyApplicantAsync(applicationType, applicationId,
                        applicantId.Value, "approved", reviewNote);
                // 請款申請核准後，額外通知財務部進行撥款
                if (applicationType == "payment_request" && applicantId.HasValue)
                    await notifier.NotifyFinanceDeptAsync(applicationId, applicantId.Value);
            }
        }
        else if (action == "returned")
        {
            setStatus("returned"); // 退回申請人修改
            setReviewed();
            // 通知申請人：已退回
            if (applicantId.HasValue)
                await notifier.NotifyApplicantAsync(applicationType, applicationId,
                    applicantId.Value, "returned", reviewNote);
        }
        else // rejected
        {
            setStatus("rejected"); // 終止
            setReviewed();
            // 通知申請人：已拒絕
            if (applicantId.HasValue)
                await notifier.NotifyApplicantAsync(applicationType, applicationId,
                    applicantId.Value, "rejected", reviewNote);
        }
    }
}
