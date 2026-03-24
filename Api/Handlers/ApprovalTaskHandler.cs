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
public sealed class ApprovalTaskHandler(AppDbContext db, IPaymentRequestReadService reader, IJwtService jwtService, IApprovalNotificationService notifier, IApprovalFlowService approvalFlow)
{
    private static readonly HashSet<string> ValidActions  = ["approved", "returned", "rejected"];
    public  static readonly HashSet<string> ValidAppTypes = ["payment_request", "leave", "travel", "overtime", "advance", "write_off", "travel_write_off"];

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

    public async Task<IActionResult> GetByIdAsync(HttpRequest req, string id, string? applicationType = null)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid approval task ID format."));

        var task = !string.IsNullOrEmpty(applicationType)
            ? await reader.GetApprovalTaskByIdAsync(intId, applicationType)
            : await reader.GetApprovalTaskByIdAsync(intId);

        if (task is null)
            return new NotFoundObjectResult(ApiResponse.Fail("Approval task not found.", $"No request with id '{id}'."));

        // ── 存取控制：只有有權審核此申請的使用者才能查看詳情 ─────────────────
        var principal = await jwtService.ValidateRequestAsync(req);
        if (principal is not null)
        {
            var isSuperAdmin = principal.FindFirst("is_superadmin")?.Value == "true";
            if (!isSuperAdmin)
            {
                var userIdStr = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                if (Guid.TryParse(userIdStr, out var callerId))
                {
                    var appType = task.ApplicationType;
                    // 1. 曾審核過（有 ApprovalRecord）
                    bool hasRecord = await db.ApprovalRecords.AsNoTracking()
                        .AnyAsync(ar => ar.ApplicationType == appType && ar.ApplicationId == intId && ar.ReviewedById == callerId);
                    // 2. 被指定為審核者（任何狀態）
                    bool isDesignated = await db.RequestDesignatedReviewers.AsNoTracking()
                        .AnyAsync(r => r.RequestType == appType && r.RequestId == intId && r.ReviewerId == callerId);
                    // 3. 符合全域審核權限（有 approval-tasks:read 可看清單的人也能看詳情）
                    bool hasReadPerm = principal.FindAll("permissions").Any(c => c.Value == PermissionCodes.ApprovalTasksRead);

                    if (!hasRecord && !isDesignated && !hasReadPerm)
                        return new ObjectResult(ApiResponse.Fail("您沒有權限查看此申請單。")) { StatusCode = 403 };
                }
            }
        }

        return new OkObjectResult(ApiResponse.Ok(task));
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

        // ── 權限檢查：指定審核步驟（UseApplicantDesignated）的被指定人不需要全域審核權限 ──────
        if (!reviewer.IsSuperAdmin)
        {
            // 先判斷是否為「被指定的審核者」
            bool isDesignatedReviewer = await db.RequestDesignatedReviewers
                .AsNoTracking()
                .AnyAsync(r => r.RequestType == applicationType
                             && r.RequestId == intId
                             && r.ReviewerId == reviewerId
                             && r.Status == "pending");

            if (!isDesignatedReviewer)
            {
                // 非指定審核者 → 必須擁有全域審核權限
                var permissions = principal.FindAll("permissions").Select(c => c.Value);
                if (!permissions.Contains(PermissionCodes.ApprovalTasksWrite))
                    return new ObjectResult(ApiResponse.Fail("缺少所需權限：approval-tasks:write")) { StatusCode = 403 };
            }
        }

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
                await AuthorizeStepAsync(pr.ApprovalItemId, pr.CurrentStepOrder, reviewer, prApplicant?.DepartmentId, "payment_request", pr.Id, prApplicant?.JobTitleId);
                // 設定預計撥款日 / 撥款日（審核者可在審核時填寫）
                if (body.EstimatedPaymentDate.HasValue)
                    pr.EstimatedPaymentDate = body.EstimatedPaymentDate.Value;
                if (body.PaidAt.HasValue)
                {
                    pr.PaidAt = body.PaidAt.Value;
                    pr.PaidByUserId = reviewerId;
                }
                await ProcessReviewAsync("payment_request", pr.Id, pr.CurrentStepOrder,
                    pr.ApprovalItemId, body.Action, body.ReviewNote, reviewerId, pr.SubmittedById,
                    setStatus:     s  => pr.ApprovalStatus   = s,
                    incrementStep: () => pr.CurrentStepOrder++,
                    setReviewed:   () => { pr.ReviewedAt = Clock.Now; pr.ReviewedById = reviewerId; pr.ReviewNote = body.ReviewNote?.Trim(); });
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
                await AuthorizeStepAsync(lr.ApprovalItemId, lr.CurrentStepOrder, reviewer, lrApplicant?.DepartmentId, "leave", lr.Id, lrApplicant?.JobTitleId);
                await ProcessReviewAsync("leave", lr.Id, lr.CurrentStepOrder,
                    lr.ApprovalItemId, body.Action, body.ReviewNote, reviewerId, lr.EmployeeId,
                    setStatus:     s  => lr.ApprovalStatus   = s,
                    incrementStep: () => lr.CurrentStepOrder++,
                    setReviewed:   () => { lr.ReviewedAt = Clock.Now; lr.ReviewedById = reviewerId; lr.ReviewNote = body.ReviewNote?.Trim(); });
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
                await AuthorizeStepAsync(tr.ApprovalItemId, tr.CurrentStepOrder, reviewer, trApplicant?.DepartmentId, "travel", tr.Id, trApplicant?.JobTitleId);
                await ProcessReviewAsync("travel", tr.Id, tr.CurrentStepOrder,
                    tr.ApprovalItemId, body.Action, body.ReviewNote, reviewerId, tr.EmployeeId,
                    setStatus:     s  => tr.ApprovalStatus   = s,
                    incrementStep: () => tr.CurrentStepOrder++,
                    setReviewed:   () => { tr.ReviewedAt = Clock.Now; tr.ReviewedById = reviewerId; tr.ReviewNote = body.ReviewNote?.Trim(); });
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
                await AuthorizeStepAsync(ot.ApprovalItemId, ot.CurrentStepOrder, reviewer, otApplicant?.DepartmentId, "overtime", ot.Id, otApplicant?.JobTitleId);
                await ProcessReviewAsync("overtime", ot.Id, ot.CurrentStepOrder,
                    ot.ApprovalItemId, body.Action, body.ReviewNote, reviewerId, ot.EmployeeId,
                    setStatus:     s  => ot.ApprovalStatus   = s,
                    incrementStep: () => ot.CurrentStepOrder++,
                    setReviewed:   () => { ot.ReviewedAt = Clock.Now; ot.ReviewedById = reviewerId; ot.ReviewNote = body.ReviewNote?.Trim(); });
                await db.SaveChangesAsync();
                break;
            }
            case "advance":
            {
                var adv = await db.AdvanceRequests.FindAsync(intId)
                    ?? throw AppException.NotFound("AdvanceRequest");
                if (adv.ApprovalStatus != "pending")
                    throw AppException.BadRequest("Only pending advance requests can be reviewed.");

                var advApplicant = adv.SubmittedById.HasValue
                    ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == adv.SubmittedById.Value)
                    : null;
                await AuthorizeStepAsync(adv.ApprovalItemId, adv.CurrentStepOrder, reviewer, advApplicant?.DepartmentId, "advance", adv.Id, advApplicant?.JobTitleId);
                if (body.EstimatedPaymentDate.HasValue)
                    adv.EstimatedPaymentDate = body.EstimatedPaymentDate.Value;
                if (body.PaidAt.HasValue)
                {
                    adv.PaidAt = body.PaidAt.Value;
                    adv.PaidByUserId = reviewerId;
                }
                await ProcessReviewAsync("advance", adv.Id, adv.CurrentStepOrder,
                    adv.ApprovalItemId, body.Action, body.ReviewNote, reviewerId, adv.SubmittedById,
                    setStatus:     s  => adv.ApprovalStatus   = s,
                    incrementStep: () => adv.CurrentStepOrder++,
                    setReviewed:   () => { adv.ReviewedAt = Clock.Now; adv.ReviewedById = reviewerId; adv.ReviewNote = body.ReviewNote?.Trim(); });
                await db.SaveChangesAsync();
                break;
            }
            case "write_off":
            {
                var wo = await db.WriteOffRecords.FindAsync(intId)
                    ?? throw AppException.NotFound("WriteOffRecord");
                if (wo.ApprovalStatus != "pending")
                    throw AppException.BadRequest("Only pending write-off records can be reviewed.");

                var woApplicant = wo.SubmittedById.HasValue
                    ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == wo.SubmittedById.Value)
                    : null;
                await AuthorizeStepAsync(wo.ApprovalItemId, wo.CurrentStepOrder, reviewer, woApplicant?.DepartmentId, "write_off", wo.Id, woApplicant?.JobTitleId);
                // 設定預支申請撥款日（審核者可在審核沖銷時填寫）
                if (body.EstimatedPaymentDate.HasValue || body.PaidAt.HasValue)
                {
                    var adv = await db.AdvanceRequests.FindAsync(wo.AdvanceRequestId);
                    if (adv is not null)
                    {
                        if (body.EstimatedPaymentDate.HasValue)
                            adv.EstimatedPaymentDate = body.EstimatedPaymentDate.Value;
                        if (body.PaidAt.HasValue)
                        {
                            adv.PaidAt = body.PaidAt.Value;
                            adv.PaidByUserId = reviewerId;
                        }
                    }
                }
                // 記住審核前的步驟（ProcessReviewAsync 可能會 increment）
                var reviewedStepOrder = wo.CurrentStepOrder;

                await ProcessReviewAsync("write_off", wo.Id, wo.CurrentStepOrder,
                    wo.ApprovalItemId, body.Action, body.ReviewNote, reviewerId, wo.SubmittedById,
                    setStatus:     s  => wo.ApprovalStatus   = s,
                    incrementStep: () => wo.CurrentStepOrder++,
                    setReviewed:   () => { wo.ReviewedAt = Clock.Now; wo.ReviewedById = reviewerId; wo.ReviewNote = body.ReviewNote?.Trim(); });

                // 預支結案：財務部步驟核准時，可勾選結案
                if (body.CloseAdvance == true && body.Action == "approved")
                {
                    // 驗證審核的步驟是否為財務部
                    var currentStep = wo.ApprovalItemId.HasValue
                        ? await db.ApprovalSteps.AsNoTracking()
                            .Include(s => s.Department)
                            .FirstOrDefaultAsync(s => s.ApprovalItemId == wo.ApprovalItemId && s.StepOrder == reviewedStepOrder)
                        : null;

                    if (currentStep?.Department?.Code == "FIN" || reviewer.IsSuperAdmin)
                    {
                        var advance = await db.AdvanceRequests.FindAsync(wo.AdvanceRequestId);
                        if (advance is not null && !advance.IsClosed)
                        {
                            advance.IsClosed   = true;
                            advance.ClosedAt   = Clock.Now;
                            advance.ClosedById = reviewerId;

                            // 檢查是否有退還差額（沖銷累計 > 預支金額）
                            var totalWrittenOff = await db.WriteOffRecords
                                .Where(w => w.AdvanceRequestId == wo.AdvanceRequestId && w.ApprovalStatus != "rejected")
                                .SumAsync(w => (decimal?)w.GrandTotal) ?? 0m;

                            var diff = totalWrittenOff - advance.GrandTotal;
                            if (diff > 0)
                            {
                                advance.RefundAmount = diff;
                                // 寄通知信給財務部全員
                                await notifier.NotifyFinanceRefundAsync(advance, diff);
                            }
                        }
                    }
                }

                await db.SaveChangesAsync();
                break;
            }
            case "travel_write_off":
            {
                var two = await db.TravelWriteOffRecords.FindAsync(intId)
                    ?? throw AppException.NotFound("TravelWriteOffRecord");
                if (two.ApprovalStatus != "pending")
                    throw AppException.BadRequest("Only pending travel write-off records can be reviewed.");

                var twoApplicant = two.SubmittedById.HasValue
                    ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == two.SubmittedById.Value)
                    : null;
                await AuthorizeStepAsync(two.ApprovalItemId, two.CurrentStepOrder, reviewer, twoApplicant?.DepartmentId, "travel_write_off", two.Id, twoApplicant?.JobTitleId);

                // 記住審核前的步驟（ProcessReviewAsync 可能會 increment）
                var twoReviewedStepOrder = two.CurrentStepOrder;

                await ProcessReviewAsync("travel_write_off", two.Id, two.CurrentStepOrder,
                    two.ApprovalItemId, body.Action, body.ReviewNote, reviewerId, two.SubmittedById,
                    setStatus:     s  => two.ApprovalStatus    = s,
                    incrementStep: () => two.CurrentStepOrder++,
                    setReviewed:   () => { two.ReviewedAt = Clock.Now; two.ReviewedById = reviewerId; two.ReviewNote = body.ReviewNote?.Trim(); });

                // 出差結案：財務部步驟核准時，可勾選結案
                if (body.CloseAdvance == true && body.Action == "approved")
                {
                    // 驗證審核的步驟是否為財務部
                    var currentStep = two.ApprovalItemId.HasValue
                        ? await db.ApprovalSteps.AsNoTracking()
                            .Include(s => s.Department)
                            .FirstOrDefaultAsync(s => s.ApprovalItemId == two.ApprovalItemId && s.StepOrder == twoReviewedStepOrder)
                        : null;

                    if (currentStep?.Department?.Code == "FIN" || reviewer.IsSuperAdmin)
                    {
                        var travel = await db.TravelRequests.FindAsync(two.TravelRequestId);
                        if (travel is not null && !travel.IsClosed)
                        {
                            travel.IsClosed   = true;
                            travel.ClosedAt   = Clock.Now;
                            travel.ClosedById = reviewerId;

                            // 檢查是否有退還差額（沖銷累計 > 出差金額）
                            var totalWrittenOff = await db.TravelWriteOffRecords
                                .Where(w => w.TravelRequestId == two.TravelRequestId && w.ApprovalStatus != "rejected")
                                .SumAsync(w => (decimal?)w.GrandTotal) ?? 0m;

                            var diff = totalWrittenOff - travel.GrandTotal;
                            if (diff > 0)
                            {
                                travel.RefundAmount = diff;
                                // 寄通知信給財務部全員
                                await notifier.NotifyFinanceTravelRefundAsync(travel, diff);
                            }
                        }
                    }
                }

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

    /// <summary>驗證當前審核者是否有權審核此步驟（依職稱/部門設定，或升級審核指派，或指定審核）。Superadmin 可審核任何步驟。</summary>
    private async Task AuthorizeStepAsync(
        int? approvalItemId, int currentStepOrder, User reviewer,
        int? applicantDepartmentId = null, string? applicationType = null, int? applicationId = null,
        int? applicantJobTitleId = null)
    {
        if (approvalItemId is null) return;

        // Superadmin 可審核任何步驟
        if (reviewer.IsSuperAdmin) return;

        var step = await db.ApprovalSteps
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ApprovalItemId == approvalItemId && s.StepOrder == currentStepOrder);

        if (step is null) return;

        // ── UseApplicantDesignated 模式：查詢 RequestDesignatedReviewers 找當前 pending 最小 StepOrder 的審核者 ──
        if (step.UseApplicantDesignated)
        {
            if (applicationType is not null && applicationId.HasValue)
            {
                var currentDesignated = await db.RequestDesignatedReviewers
                    .AsNoTracking()
                    .Where(r => r.RequestType == applicationType
                             && r.RequestId == applicationId.Value
                             && r.Status == "pending")
                    .OrderBy(r => r.StepOrder)
                    .Select(r => r.ReviewerId)
                    .FirstOrDefaultAsync();

                if (currentDesignated != Guid.Empty && reviewer.Id == currentDesignated)
                    return; // 授權通過
            }
            throw AppException.Forbidden("You are not authorized to review this step.");
        }

        // ── UseDirectSupervisor 模式：驗證審核者是同部門的第 N 層上級 ──
        if (step.UseDirectSupervisor)
        {
            if (applicantDepartmentId is null || applicantJobTitleId is null
                || reviewer.DepartmentId != applicantDepartmentId
                || reviewer.JobTitleId is null)
                throw AppException.Forbidden("You are not authorized to review this step.");

            // 計算 rank：此步驟前有幾個 UseDirectSupervisor 步驟
            var rank = await db.ApprovalSteps.AsNoTracking()
                .CountAsync(s => s.ApprovalItemId == approvalItemId
                    && s.UseDirectSupervisor
                    && s.StepOrder < currentStepOrder);

            var applicantLevel = await db.JobTitles.AsNoTracking()
                .Where(j => j.Id == applicantJobTitleId).Select(j => j.Level).FirstOrDefaultAsync();
            var reviewerLevel = await db.JobTitles.AsNoTracking()
                .Where(j => j.Id == reviewer.JobTitleId).Select(j => j.Level).FirstOrDefaultAsync();

            // 找第 N 層上級的目標 Level
            var targetLevel = await db.Users.AsNoTracking()
                .Where(u => u.DepartmentId == applicantDepartmentId
                    && !u.IsSuperAdmin
                    && u.JobTitle != null
                    && u.JobTitle.Level < applicantLevel)
                .Select(u => u.JobTitle!.Level)
                .Distinct()
                .OrderByDescending(l => l) // 最接近的排前面
                .Skip(rank)
                .FirstOrDefaultAsync();

            if (targetLevel == 0 || reviewerLevel != targetLevel)
                throw AppException.Forbidden("You are not authorized to review this step.");

            return; // 授權通過
        }

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
        Action<string> setStatus, Action incrementStep, Action setReviewed,
        Action<int>? setStepOrder = null)
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
            // ── UseApplicantDesignated 步驟：處理多位指定審核者的逐一推進 ──
            var currentStepDef = approvalItemId.HasValue
                ? await db.ApprovalSteps.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ApprovalItemId == approvalItemId && s.StepOrder == currentStepOrder)
                : null;

            if (currentStepDef?.UseApplicantDesignated == true)
            {
                // 更新當前 pending 且 StepOrder 最小的指定審核者狀態為 approved
                var currentDesignated = await db.RequestDesignatedReviewers
                    .Where(r => r.RequestType == applicationType
                             && r.RequestId == applicationId
                             && r.Status == "pending")
                    .OrderBy(r => r.StepOrder)
                    .FirstOrDefaultAsync();

                if (currentDesignated is not null)
                {
                    currentDesignated.Status     = "approved";
                    currentDesignated.ReviewedAt = Clock.Now;
                    currentDesignated.Comment    = reviewNote?.Trim();
                }

                // 檢查是否還有下一位指定審核者（StepOrder 比當前更大且狀態仍為 pending）
                // 注意：不能直接過濾 Status == "pending" 來找「比當前更大」的記錄，
                // 因為 currentDesignated.Status 已在記憶體中更新為 "approved"，
                // 但尚未 SaveChanges，資料庫中仍為 "pending"。
                // 以 StepOrder > currentDesignated.StepOrder 避免誤查回同一筆記錄。
                int currentStepOrderDR = currentDesignated?.StepOrder ?? -1;
                var nextDesignated = await db.RequestDesignatedReviewers
                    .AsNoTracking()
                    .Where(r => r.RequestType == applicationType
                             && r.RequestId == applicationId
                             && r.Status == "pending"
                             && r.StepOrder > currentStepOrderDR)
                    .OrderBy(r => r.StepOrder)
                    .FirstOrDefaultAsync();

                if (nextDesignated is not null)
                {
                    // 還有下一位指定審核者：保持 CurrentStepOrder 不變，通知下一位
                    await db.SaveChangesAsync();
                    if (applicantId.HasValue)
                        await notifier.NotifySpecificReviewerAsync(applicationType, applicationId,
                            nextDesignated.ReviewerId, applicantId.Value, false);
                    return; // 不繼續後面的推進邏輯
                }
                // 所有指定審核者都 approved → 繼續原有推進到下一 ApprovalStep 的邏輯
            }

            // action == "returned" 時的指定審核者處理在下方 else if 區塊

            // 查詢下一個步驟（用 StepOrder 找，支援稀疏 StepOrder）
            var nextStepEntity = approvalItemId.HasValue
                ? await db.ApprovalSteps.AsNoTracking()
                    .Where(s => s.ApprovalItemId == approvalItemId && s.StepOrder > currentStepOrder)
                    .OrderBy(s => s.StepOrder)
                    .FirstOrDefaultAsync()
                : null;

            if (nextStepEntity is not null)
            {
                int nextStep = nextStepEntity.StepOrder;

                // 檢查後續步驟是否有審核者，跳過無人的步驟
                if (applicantId.HasValue)
                {
                    // 查詢此申請單的指定審核者清單（用於 SkipUnreviewableStepsAsync 判斷 UseApplicantDesignated 步驟）
                    var drList = await db.RequestDesignatedReviewers
                        .AsNoTracking()
                        .Where(r => r.RequestType == applicationType && r.RequestId == applicationId)
                        .OrderBy(r => r.StepOrder)
                        .Select(r => new DesignatedReviewerRequest(r.ReviewerId, r.StepOrder))
                        .ToListAsync();

                    var (resolvedStep, allSkipped) = await approvalFlow
                        .SkipUnreviewableStepsAsync(approvalItemId, applicantId.Value, nextStep, drList);

                    if (allSkipped)
                    {
                        // 所有剩餘步驟都跳過 → 直接核准
                        if (setStepOrder is not null) setStepOrder(resolvedStep);
                        else { for (int i = currentStepOrder; i < resolvedStep; i++) incrementStep(); }
                        setStatus("approved");
                        setReviewed();
                        await notifier.NotifyApplicantAsync(applicationType, applicationId,
                            applicantId.Value, "approved", reviewNote);
                        return;
                    }

                    // 設定 CurrentStepOrder 到下一個有效步驟
                    if (setStepOrder is not null)
                    {
                        setStepOrder(resolvedStep);
                    }
                    else
                    {
                        // incrementStep 只能 +1，需要用迴圈或直接設值
                        // 為了安全，先用 incrementStep 到目標
                        for (int i = currentStepOrder; i < resolvedStep; i++) incrementStep();
                    }
                    nextStep = resolvedStep;
                }
                else
                {
                    if (setStepOrder is not null) setStepOrder(nextStep);
                    else { for (int i = currentStepOrder; i < nextStep; i++) incrementStep(); }
                }

                // 通知下一步審核者
                if (applicantId.HasValue)
                    await notifier.NotifyReviewersAsync(applicationType, applicationId,
                        approvalItemId, nextStep, applicantId.Value);
            }
            else
            {
                setStatus("approved"); // 所有步驟完成
                setReviewed();
                // 通知申請人：已核准
                if (applicantId.HasValue)
                    await notifier.NotifyApplicantAsync(applicationType, applicationId,
                        applicantId.Value, "approved", reviewNote);
            }
        }
        else if (action == "returned")
        {
            // UseApplicantDesignated 步驟退回時：更新當前 pending 指定審核者的狀態為 returned
            var currentStepDefForReturn = approvalItemId.HasValue
                ? await db.ApprovalSteps.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ApprovalItemId == approvalItemId && s.StepOrder == currentStepOrder)
                : null;

            if (currentStepDefForReturn?.UseApplicantDesignated == true)
            {
                var currentDesignatedForReturn = await db.RequestDesignatedReviewers
                    .Where(r => r.RequestType == applicationType
                             && r.RequestId == applicationId
                             && r.Status == "pending")
                    .OrderBy(r => r.StepOrder)
                    .FirstOrDefaultAsync();

                if (currentDesignatedForReturn is not null)
                {
                    currentDesignatedForReturn.Status     = "returned";
                    currentDesignatedForReturn.ReviewedAt = Clock.Now;
                    currentDesignatedForReturn.Comment    = reviewNote?.Trim();
                }
            }

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
