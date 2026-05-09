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
    public  static readonly HashSet<string> ValidAppTypes = ["payment_request", "leave", "travel", "overtime", "advance", "write_off", "travel_write_off", "holiday_travel", "travel_payment"];

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
        string? paymentStatus = req.Query["paymentStatus"].ToString() is { Length: > 0 } ps2 ? ps2 : null;
        // 類型篩選：須為 ValidAppTypes 之一，否則忽略
        string? applicationType = req.Query["applicationType"].ToString() is { Length: > 0 } at && ValidAppTypes.Contains(at) ? at : null;

        var allTasks = (await reader.GetApprovalTasksAsync(jobTitleId, deptId, status, reviewerUserId, paymentStatus, applicationType)).ToList();
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
        await ReviewOneEntityAsync(
            applicationType, intId, reviewer, reviewerId,
            body.Action, body.ReviewNote,
            body.EstimatedPaymentDate, body.PaidAt, body.CloseAdvance);

        var task = await reader.GetApprovalTaskByIdAsync(intId, applicationType);
        return new OkObjectResult(ApiResponse.Ok(task, $"Request {body.Action}."));
    }

    /// <summary>
    /// 批次核准多筆待審申請（僅支援 approved 動作）。
    /// 每筆仍獨立走 AuthorizeStepAsync 驗證；單筆失敗不影響其他，失敗項目回報於 Failed。
    /// 核准後若有需補填撥款/退款日的申請，回傳於 PendingPayment 供前端提醒。
    /// </summary>
    public async Task<IActionResult> BatchApproveAsync(HttpRequest req)
    {
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

        // ── 權限檢查：非 superadmin 必須擁有 approval-tasks:batch-approve 權限 ──
        if (!reviewer.IsSuperAdmin)
        {
            var permissions = principal.FindAll("permissions").Select(c => c.Value);
            if (!permissions.Contains(PermissionCodes.ApprovalTasksBatchApprove))
                return new ObjectResult(ApiResponse.Fail("缺少所需權限：approval-tasks:batch-approve")) { StatusCode = 403 };
        }

        // ── 讀取請求內容 ───────────────────────────────────────────────────
        var body = await req.ReadFromJsonAsync<BatchApproveRequest>();
        if (body is null || body.Items is null || body.Items.Count == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("批次核准清單不可為空。"));

        var succeeded = 0;
        var failed    = new List<BatchApproveFailure>();
        var pending   = new List<BatchApprovePending>();

        foreach (var item in body.Items)
        {
            if (!ValidAppTypes.Contains(item.ApplicationType))
            {
                failed.Add(new BatchApproveFailure(item.ApplicationType, item.Id, "未知的申請類型"));
                continue;
            }

            try
            {
                await ReviewOneEntityAsync(
                    item.ApplicationType, item.Id, reviewer, reviewerId,
                    action: "approved", reviewNote: null,
                    estimatedPaymentDate: null, paidAt: null, closeAdvance: null);
                succeeded++;

                // 若核准後該申請已變成最終 approved 且需補填撥款/退款日，加入提醒清單
                var reminder = await BuildPendingPaymentReminderAsync(item.ApplicationType, item.Id);
                if (reminder is not null)
                    pending.Add(reminder);
            }
            catch (AppException ex)
            {
                failed.Add(new BatchApproveFailure(item.ApplicationType, item.Id, ex.Message));
            }
            catch (Exception)
            {
                failed.Add(new BatchApproveFailure(item.ApplicationType, item.Id, "批次核准失敗"));
            }
        }

        return new OkObjectResult(ApiResponse.Ok(new BatchApproveResult(succeeded, failed, pending)));
    }

    /// <summary>
    /// 單筆核准/退回/拒絕的核心邏輯：依申請類型載入實體、驗證審核者、寫入審核紀錄、推進步驟、SaveChanges。
    /// 由 <see cref="ReviewAsync"/> 與 <see cref="BatchApproveAsync"/> 共用。
    /// </summary>
    private async Task ReviewOneEntityAsync(
        string    applicationType,
        int       intId,
        User      reviewer,
        Guid      reviewerId,
        string    action,
        string?   reviewNote,
        DateTime? estimatedPaymentDate,
        DateTime? paidAt,
        bool?     closeAdvance)
    {
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
                if (estimatedPaymentDate.HasValue)
                    pr.EstimatedPaymentDate = estimatedPaymentDate.Value;
                if (paidAt.HasValue)
                {
                    pr.PaidAt = paidAt.Value;
                    pr.PaidByUserId = reviewerId;
                }
                await ProcessReviewAsync("payment_request", pr.Id, pr.CurrentStepOrder,
                    pr.ApprovalItemId, action, reviewNote, reviewerId, pr.SubmittedById,
                    setStatus:     s  => pr.ApprovalStatus   = s,
                    incrementStep: () => pr.CurrentStepOrder++,
                    setReviewed:   () => { pr.ReviewedAt = Clock.Now; pr.ReviewedById = reviewerId; pr.ReviewNote = reviewNote?.Trim(); });
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
                    lr.ApprovalItemId, action, reviewNote, reviewerId, lr.EmployeeId,
                    setStatus:     s  => lr.ApprovalStatus   = s,
                    incrementStep: () => lr.CurrentStepOrder++,
                    setReviewed:   () => { lr.ReviewedAt = Clock.Now; lr.ReviewedById = reviewerId; lr.ReviewNote = reviewNote?.Trim(); });
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
                    tr.ApprovalItemId, action, reviewNote, reviewerId, tr.EmployeeId,
                    setStatus:     s  => tr.ApprovalStatus   = s,
                    incrementStep: () => tr.CurrentStepOrder++,
                    setReviewed:   () => { tr.ReviewedAt = Clock.Now; tr.ReviewedById = reviewerId; tr.ReviewNote = reviewNote?.Trim(); });
                await db.SaveChangesAsync();
                break;
            }
            case "holiday_travel":
            {
                var htr = await db.TravelRequests.FindAsync(intId)
                    ?? throw AppException.NotFound("TravelRequest");
                if (htr.ApprovalStatus != "pending")
                    throw AppException.BadRequest("Only pending travel requests can be reviewed.");

                var htrApplicant = htr.EmployeeId.HasValue
                    ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == htr.EmployeeId.Value)
                    : null;
                await AuthorizeStepAsync(htr.ApprovalItemId, htr.CurrentStepOrder, reviewer, htrApplicant?.DepartmentId, "holiday_travel", htr.Id, htrApplicant?.JobTitleId);
                await ProcessReviewAsync("holiday_travel", htr.Id, htr.CurrentStepOrder,
                    htr.ApprovalItemId, action, reviewNote, reviewerId, htr.EmployeeId,
                    setStatus:     s  => htr.ApprovalStatus   = s,
                    incrementStep: () => htr.CurrentStepOrder++,
                    setReviewed:   () => { htr.ReviewedAt = Clock.Now; htr.ReviewedById = reviewerId; htr.ReviewNote = reviewNote?.Trim(); });
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
                    ot.ApprovalItemId, action, reviewNote, reviewerId, ot.EmployeeId,
                    setStatus:     s  => ot.ApprovalStatus   = s,
                    incrementStep: () => ot.CurrentStepOrder++,
                    setReviewed:   () => { ot.ReviewedAt = Clock.Now; ot.ReviewedById = reviewerId; ot.ReviewNote = reviewNote?.Trim(); });
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
                if (estimatedPaymentDate.HasValue)
                    adv.EstimatedPaymentDate = estimatedPaymentDate.Value;
                if (paidAt.HasValue)
                    adv.RefundedAt = paidAt.Value;
                await ProcessReviewAsync("advance", adv.Id, adv.CurrentStepOrder,
                    adv.ApprovalItemId, action, reviewNote, reviewerId, adv.SubmittedById,
                    setStatus:     s  => adv.ApprovalStatus   = s,
                    incrementStep: () => adv.CurrentStepOrder++,
                    setReviewed:   () => { adv.ReviewedAt = Clock.Now; adv.ReviewedById = reviewerId; adv.ReviewNote = reviewNote?.Trim(); });
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
                // 設定預支申請退款日（審核者可在審核沖銷時填寫）
                if (estimatedPaymentDate.HasValue || paidAt.HasValue)
                {
                    var adv = await db.AdvanceRequests.FindAsync(wo.AdvanceRequestId);
                    if (adv is not null)
                    {
                        if (estimatedPaymentDate.HasValue)
                            adv.EstimatedRefundDate = estimatedPaymentDate.Value;
                        if (paidAt.HasValue)
                        {
                            adv.RefundedAt = paidAt.Value;
                            adv.RefundedByUserId = reviewerId;
                        }
                    }
                }
                // 記住審核前的步驟（ProcessReviewAsync 可能會 increment）
                var reviewedStepOrder = wo.CurrentStepOrder;

                await ProcessReviewAsync("write_off", wo.Id, wo.CurrentStepOrder,
                    wo.ApprovalItemId, action, reviewNote, reviewerId, wo.SubmittedById,
                    setStatus:     s  => wo.ApprovalStatus   = s,
                    incrementStep: () => wo.CurrentStepOrder++,
                    setReviewed:   () => { wo.ReviewedAt = Clock.Now; wo.ReviewedById = reviewerId; wo.ReviewNote = reviewNote?.Trim(); });

                // 預支結案：財務部步驟核准時，可勾選結案
                if (closeAdvance == true && action == "approved")
                {
                    // 驗證審核的步驟是否為財務部
                    var currentStep = wo.ApprovalItemId.HasValue
                        ? await db.ApprovalSteps.AsNoTracking()
                            .Include(s => s.Department)
                            .FirstOrDefaultAsync(s => s.ApprovalItemId == wo.ApprovalItemId && s.StepOrder == reviewedStepOrder)
                        : null;

                    if (currentStep?.Department?.Code == "FIN" || reviewer.IsSuperAdmin)
                        await CloseAdvanceRequestAsync(reviewerId, wo.AdvanceRequestId);
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

                // 設定出差申請退款日（審核者可在審核出差沖銷時填寫）
                if (estimatedPaymentDate.HasValue || paidAt.HasValue)
                {
                    var travel = await db.TravelRequests.FindAsync(two.TravelRequestId);
                    if (travel is not null)
                    {
                        if (estimatedPaymentDate.HasValue)
                            travel.EstimatedRefundDate = estimatedPaymentDate.Value;
                        if (paidAt.HasValue)
                        {
                            travel.RefundedAt = paidAt.Value;
                            travel.RefundedByUserId = reviewerId;
                        }
                    }
                }

                // 記住審核前的步驟（ProcessReviewAsync 可能會 increment）
                var twoReviewedStepOrder = two.CurrentStepOrder;

                await ProcessReviewAsync("travel_write_off", two.Id, two.CurrentStepOrder,
                    two.ApprovalItemId, action, reviewNote, reviewerId, two.SubmittedById,
                    setStatus:     s  => two.ApprovalStatus    = s,
                    incrementStep: () => two.CurrentStepOrder++,
                    setReviewed:   () => { two.ReviewedAt = Clock.Now; two.ReviewedById = reviewerId; two.ReviewNote = reviewNote?.Trim(); });

                // 出差結案：財務部步驟核准時，可勾選結案
                if (closeAdvance == true && action == "approved")
                {
                    // 驗證審核的步驟是否為財務部
                    var currentStep = two.ApprovalItemId.HasValue
                        ? await db.ApprovalSteps.AsNoTracking()
                            .Include(s => s.Department)
                            .FirstOrDefaultAsync(s => s.ApprovalItemId == two.ApprovalItemId && s.StepOrder == twoReviewedStepOrder)
                        : null;

                    if (currentStep?.Department?.Code == "FIN" || reviewer.IsSuperAdmin)
                        await CloseTravelRequestAsync(reviewerId, two.TravelRequestId);
                }

                await db.SaveChangesAsync();
                break;
            }
            case "travel_payment":
            {
                var tpr = await db.TravelPaymentRequests.FindAsync(intId)
                    ?? throw AppException.NotFound("TravelPaymentRequest");
                if (tpr.ApprovalStatus != "pending")
                    throw AppException.BadRequest("Only pending travel payment requests can be reviewed.");

                var tprApplicant = tpr.EmployeeId.HasValue
                    ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == tpr.EmployeeId.Value)
                    : null;
                await AuthorizeStepAsync(tpr.ApprovalItemId, tpr.CurrentStepOrder, reviewer, tprApplicant?.DepartmentId, "travel_payment", tpr.Id, tprApplicant?.JobTitleId);
                if (estimatedPaymentDate.HasValue)
                    tpr.EstimatedPaymentDate = estimatedPaymentDate.Value;
                if (paidAt.HasValue)
                {
                    tpr.PaidAt        = paidAt.Value;
                    tpr.PaidByUserId  = reviewerId;
                }
                await ProcessReviewAsync("travel_payment", tpr.Id, tpr.CurrentStepOrder,
                    tpr.ApprovalItemId, action, reviewNote, reviewerId, tpr.EmployeeId,
                    setStatus:     s  => tpr.ApprovalStatus   = s,
                    incrementStep: () => tpr.CurrentStepOrder++,
                    setReviewed:   () => { tpr.ReviewedAt = Clock.Now; tpr.ReviewedById = reviewerId; tpr.ReviewNote = reviewNote?.Trim(); });
                await db.SaveChangesAsync();
                break;
            }
            default:
                throw AppException.BadRequest("Unknown application type.");
        }
    }

    /// <summary>
    /// 批次核准後，若該申請已變成最終 approved 且尚未填寫撥款/退款日，回傳提醒；否則回傳 null。
    /// - payment_request / advance / travel / holiday_travel：PaidAt 為空 → kind="payment"
    /// - write_off / travel_write_off：尚未支援（需依 CloseAdvance 流程，批次核准不觸發）→ 回 null
    /// </summary>
    private async Task<BatchApprovePending?> BuildPendingPaymentReminderAsync(string applicationType, int id)
    {
        switch (applicationType)
        {
            case "payment_request":
            {
                var pr = await db.PaymentRequests.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (pr is not null && pr.ApprovalStatus == "approved" && pr.PaidAt is null)
                    return new BatchApprovePending("payment_request", id, $"#{id}", "payment");
                return null;
            }
            case "advance":
            {
                var adv = await db.AdvanceRequests.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (adv is not null && adv.ApprovalStatus == "approved" && adv.PaidAt is null)
                    return new BatchApprovePending("advance", id, adv.RequestNo, "payment");
                return null;
            }
            case "travel":
            case "holiday_travel":
            {
                var tr = await db.TravelRequests.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (tr is not null && tr.ApprovalStatus == "approved" && tr.PaidAt is null)
                    return new BatchApprovePending(applicationType, id, $"#{id}", "payment");
                return null;
            }
            case "travel_payment":
            {
                var tpr = await db.TravelPaymentRequests.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (tpr is not null && tpr.ApprovalStatus == "approved" && tpr.PaidAt is null)
                    return new BatchApprovePending("travel_payment", id, $"#{id}", "payment");
                return null;
            }
            default:
                return null;
        }
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

        // 跨步驟同人去重防呆（限縮版）：僅當 reviewer 為「總監（JobTitle.Level=1）」時，
        // 已審過則不允許再次審核（總監絕不重審，留一道兜底）。非總監放寬以對齊 SkipUnreviewableStepsAsync 的限縮邏輯。
        if (applicationType is not null && applicationId.HasValue)
        {
            var lastReturnedAt = await db.ApprovalRecords.AsNoTracking()
                .Where(r => r.ApplicationType == applicationType
                         && r.ApplicationId == applicationId.Value
                         && r.Action == "returned")
                .MaxAsync(r => (DateTime?)r.ReviewedAt) ?? DateTime.MinValue;

            bool alreadyApproved = await db.ApprovalRecords.AsNoTracking()
                .AnyAsync(r => r.ApplicationType == applicationType
                            && r.ApplicationId == applicationId.Value
                            && r.Action == "approved"
                            && r.ReviewedById == reviewer.Id
                            && r.ReviewedAt > lastReturnedAt);

            if (alreadyApproved)
            {
                var reviewerLevel = await db.JobTitles.AsNoTracking()
                    .Where(j => j.Id == reviewer.JobTitleId)
                    .Select(j => (int?)j.Level)
                    .FirstOrDefaultAsync();
                if (reviewerLevel == 1)
                    throw AppException.BadRequest("您已在先前步驟核准過此申請，不需重複審核。");
                // 非總監：允許重審（依新規則只有「總監 OR 相鄰 step 同人」才會被自動跳過；
                // 跳過邏輯在 ProcessReviewAsync 推進時處理，此處只防總監二度審）
            }
        }

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

                // 取得歷史已審者集合（含當下 reviewer），用於自動代簽判斷
                var approvedIdsForDesignated = await approvalFlow
                    .GetApprovedReviewerIdsAsync(applicationType, applicationId);
                approvedIdsForDesignated.Add(reviewerId);

                // 跨步驟同人去重：下一位 designee 若已於先前步驟核准過 → 自動代簽，繼續找下一位。
                // 注意：不能直接過濾 Status == "pending" 來找「比當前更大」的記錄，
                // 因為 currentDesignated.Status 已在記憶體中更新為 "approved"，
                // 但尚未 SaveChanges，資料庫中仍為 "pending"。
                // 以 StepOrder > currentDesignated.StepOrder 避免誤查回同一筆記錄。
                int currentStepOrderDR = currentDesignated?.StepOrder ?? -1;

                while (true)
                {
                    var nextDesignated = await db.RequestDesignatedReviewers
                        .Where(r => r.RequestType == applicationType
                                 && r.RequestId == applicationId
                                 && r.Status == "pending"
                                 && r.StepOrder > currentStepOrderDR)
                        .OrderBy(r => r.StepOrder)
                        .FirstOrDefaultAsync();

                    if (nextDesignated is null)
                        break; // 沒有下一位，跳出 → 推進到下一 ApprovalStep

                    if (approvedIdsForDesignated.Contains(nextDesignated.ReviewerId))
                    {
                        // 自動代簽：標記 designee approved + 寫一筆代簽 ApprovalRecord
                        nextDesignated.Status     = "approved";
                        nextDesignated.ReviewedAt = Clock.Now;
                        nextDesignated.Comment    = "已於先前步驟審核（自動核准）";

                        db.ApprovalRecords.Add(new ApprovalRecord
                        {
                            ApplicationType  = applicationType,
                            ApplicationId    = applicationId,
                            StepOrder        = currentStepOrder,
                            Action           = "approved",
                            ReviewedById     = nextDesignated.ReviewerId,
                            ReviewedAt       = Clock.Now,
                            ReviewNote       = "自動核准：已於先前步驟審核",
                            IsEscalated      = false,
                        });

                        currentStepOrderDR = nextDesignated.StepOrder;
                        continue; // 找再下一位
                    }

                    // 下一位是新人 → 通知並結束
                    await db.SaveChangesAsync();
                    if (applicantId.HasValue)
                        await notifier.NotifySpecificReviewerAsync(applicationType, applicationId,
                            nextDesignated.ReviewerId, applicantId.Value, false);
                    return; // 不繼續後面的推進邏輯
                }
                // 所有指定審核者都 approved（含自動代簽）→ 繼續原有推進到下一 ApprovalStep 的邏輯
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

                    // 取得歷史已審者集合（含當下 reviewer 與 designated while-loop 中已自動代簽者）
                    // 注意：尚未 SaveChanges，但 ChangeTracker 中的 ApprovalRecord 還沒寫入 DB，
                    // GetApprovedReviewerIdsAsync 是用 AsNoTracking 讀 DB，看不到尚未 save 的紀錄。
                    // 因此手動把當前 ChangeTracker 中 Action='approved' 的 ReviewedById 加入集合。
                    var approvedIds = await approvalFlow
                        .GetApprovedReviewerIdsAsync(applicationType, applicationId);
                    var pendingApprovedIds = db.ChangeTracker.Entries<ApprovalRecord>()
                        .Where(e => e.State == EntityState.Added
                                 && e.Entity.ApplicationType == applicationType
                                 && e.Entity.ApplicationId == applicationId
                                 && e.Entity.Action == "approved"
                                 && e.Entity.ReviewedById.HasValue)
                        .Select(e => e.Entity.ReviewedById!.Value)
                        .ToHashSet();
                    foreach (var id in pendingApprovedIds)
                        approvedIds.Add(id);

                    // 同人去重新規則：取「總監（JobTitle.Level=1）」歷史已審者集合 + ChangeTracker 中尚未 save 但已是總監的代簽人
                    var supervisorIds = await approvalFlow
                        .GetApprovedSupervisorIdsAsync(applicationType, applicationId);
                    if (pendingApprovedIds.Count > 0)
                    {
                        var pendingSupervisorIds = await db.Users.AsNoTracking()
                            .Where(u => pendingApprovedIds.Contains(u.Id)
                                     && u.JobTitle != null
                                     && u.JobTitle.Level == 1)
                            .Select(u => u.Id)
                            .ToListAsync();
                        foreach (var id in pendingSupervisorIds)
                            supervisorIds.Add(id);
                    }

                    var (resolvedStep, allSkipped, skippedSteps) = await approvalFlow
                        .SkipUnreviewableStepsAsync(approvalItemId, applicantId.Value, nextStep, drList,
                            approvedIds, applicationType, applicationId,
                            supervisorIds: supervisorIds, priorStepOrder: currentStepOrder);

                    // 對被自動跳過的 step 寫代簽 ApprovalRecord（PDF / 簽核時間軸需要）
                    foreach (var skipped in skippedSteps)
                    {
                        db.ApprovalRecords.Add(new ApprovalRecord
                        {
                            ApplicationType = applicationType,
                            ApplicationId   = applicationId,
                            StepOrder       = skipped.StepOrder,
                            Action          = "approved",
                            ReviewedById    = skipped.ProxyApproverId,
                            ReviewedAt      = Clock.Now,
                            ReviewNote      = $"自動核准：已於先前步驟核准本申請",
                            IsEscalated     = false,
                        });

                        // designated step 整步跳過時，把整個申請的 pending designee 都設為 approved
                        if (skipped.IsApplicantDesignated)
                        {
                            var stepPendingDesignees = await db.RequestDesignatedReviewers
                                .Where(r => r.RequestType == applicationType
                                         && r.RequestId == applicationId
                                         && r.Status == "pending")
                                .ToListAsync();
                            foreach (var d in stepPendingDesignees)
                            {
                                d.Status     = "approved";
                                d.ReviewedAt = Clock.Now;
                                d.Comment    = "已於先前步驟審核（自動核准）";
                            }
                        }
                    }

                    if (allSkipped)
                    {
                        // 所有剩餘步驟都跳過 → 直接核准
                        if (setStepOrder is not null) setStepOrder(resolvedStep);
                        else { for (int i = currentStepOrder; i < resolvedStep; i++) incrementStep(); }
                        setStatus("approved");
                        setReviewed();
                        await notifier.NotifyApplicantAsync(applicationType, applicationId,
                            applicantId.Value, "approved", reviewNote);
                        if (IsFinanceApplicationType(applicationType))
                            await notifier.NotifyFinanceDeptAsync(applicationId, applicantId.Value, applicationType);
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
                {
                    await notifier.NotifyApplicantAsync(applicationType, applicationId,
                        applicantId.Value, "approved", reviewNote);
                    if (IsFinanceApplicationType(applicationType))
                        await notifier.NotifyFinanceDeptAsync(applicationId, applicantId.Value, applicationType);
                }
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

    // ── 結案 Helpers ─────────────────────────────────────────────────────────

    /// <summary>關閉預支申請：設 IsClosed、計算退款差額、通知財務。</summary>
    private async Task CloseAdvanceRequestAsync(Guid closedById, int advanceRequestId)
    {
        var advance = await db.AdvanceRequests.FindAsync(advanceRequestId);
        if (advance is null || advance.IsClosed) return;

        advance.IsClosed   = true;
        advance.ClosedAt   = Clock.Now;
        advance.ClosedById = closedById;

        // 檢查是否有退還差額（沖銷累計 > 預支金額）
        var totalWrittenOff = await db.WriteOffRecords
            .Where(w => w.AdvanceRequestId == advanceRequestId && w.ApprovalStatus != "rejected")
            .SumAsync(w => (decimal?)w.GrandTotal) ?? 0m;

        var diff = totalWrittenOff - advance.GrandTotal;
        if (diff > 0)
        {
            advance.RefundAmount = diff;
            await notifier.NotifyFinanceRefundAsync(advance, diff);
        }
    }

    /// <summary>關閉出差申請：設 IsClosed、計算退款差額、通知財務。</summary>
    private async Task CloseTravelRequestAsync(Guid closedById, int travelRequestId)
    {
        var travel = await db.TravelRequests.FindAsync(travelRequestId);
        if (travel is null || travel.IsClosed) return;

        travel.IsClosed   = true;
        travel.ClosedAt   = Clock.Now;
        travel.ClosedById = closedById;

        // 檢查是否有退還差額（沖銷累計 > 出差金額）
        var totalWrittenOff = await db.TravelWriteOffRecords
            .Where(w => w.TravelRequestId == travelRequestId && w.ApprovalStatus != "rejected")
            .SumAsync(w => (decimal?)w.GrandTotal) ?? 0m;

        var diff = totalWrittenOff - travel.GrandTotal;
        if (diff > 0)
        {
            travel.RefundAmount = diff;
            await notifier.NotifyFinanceTravelRefundAsync(travel, diff);
        }
    }

    // ── 獨立結案端點 ─────────────────────────────────────────────────────────

    /// <summary>預支沖銷已核准後，財務部可獨立結案預支申請。</summary>
    public async Task<IActionResult> CloseWriteOffAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid ID format."));

        var principal = await jwtService.ValidateRequestAsync(req);
        if (principal is null)
            return new UnauthorizedObjectResult(ApiResponse.Fail("Unauthorized."));

        var userIdStr = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return new UnauthorizedObjectResult(ApiResponse.Fail("Invalid token claims."));

        var user = await db.Users.AsNoTracking().Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return new UnauthorizedObjectResult(ApiResponse.Fail("User not found."));

        if (!user.IsSuperAdmin && !DepartmentCodes.FinancialAndAbove.Contains(user.Department?.Code ?? ""))
            return new ObjectResult(ApiResponse.Fail("僅財務體系部門或 Superadmin 可執行結案操作。")) { StatusCode = 403 };

        var wo = await db.WriteOffRecords.AsNoTracking().FirstOrDefaultAsync(w => w.Id == intId);
        if (wo is null)
            return new NotFoundObjectResult(ApiResponse.Fail("Write-off record not found."));

        if (wo.ApprovalStatus != "approved")
            return new BadRequestObjectResult(ApiResponse.Fail("僅已核准的沖銷申請可執行結案。"));

        await CloseAdvanceRequestAsync(userId, wo.AdvanceRequestId);
        await db.SaveChangesAsync();

        var task = await reader.GetApprovalTaskByIdAsync(intId, "write_off");
        return new OkObjectResult(ApiResponse.Ok(task, "預支申請已結案。"));
    }

    /// <summary>出差沖銷已核准後，財務部可獨立結案出差申請。</summary>
    public async Task<IActionResult> CloseTravelWriteOffAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid ID format."));

        var principal = await jwtService.ValidateRequestAsync(req);
        if (principal is null)
            return new UnauthorizedObjectResult(ApiResponse.Fail("Unauthorized."));

        var userIdStr = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return new UnauthorizedObjectResult(ApiResponse.Fail("Invalid token claims."));

        var user = await db.Users.AsNoTracking().Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return new UnauthorizedObjectResult(ApiResponse.Fail("User not found."));

        if (!user.IsSuperAdmin && !DepartmentCodes.FinancialAndAbove.Contains(user.Department?.Code ?? ""))
            return new ObjectResult(ApiResponse.Fail("僅財務體系部門或 Superadmin 可執行結案操作。")) { StatusCode = 403 };

        var two = await db.TravelWriteOffRecords.AsNoTracking().FirstOrDefaultAsync(w => w.Id == intId);
        if (two is null)
            return new NotFoundObjectResult(ApiResponse.Fail("Travel write-off record not found."));

        if (two.ApprovalStatus != "approved")
            return new BadRequestObjectResult(ApiResponse.Fail("僅已核准的沖銷申請可執行結案。"));

        await CloseTravelRequestAsync(userId, two.TravelRequestId);
        await db.SaveChangesAsync();

        var task = await reader.GetApprovalTaskByIdAsync(intId, "travel_write_off");
        return new OkObjectResult(ApiResponse.Ok(task, "出差申請已結案。"));
    }

    /// <summary>
    /// 判斷申請類型是否屬於財務撥款範疇（最終核准後需通知財務部進行撥款作業）。
    /// 排除：leave / overtime / write_off / travel_write_off（沖銷類撥款已在預支階段完成，超額另由 Refund 通知處理）。
    /// </summary>
    private static bool IsFinanceApplicationType(string applicationType) =>
        applicationType is "payment_request"
                        or "advance"
                        or "travel"
                        or "travel_payment";
}
