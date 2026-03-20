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

public sealed class AdvanceRequestHandler(
    AppDbContext db,
    IAdvanceRequestReadService reader,
    IJwtService jwtService,
    IApprovalNotificationService notifier,
    IApprovalFlowService approvalFlow)
{
    // ── 列表（分頁）────────────────────────────────────────────────────────────

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

    // ── 單筆查詢 ────────────────────────────────────────────────────────────

    public async Task<IActionResult> GetByIdAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advance request ID format."));

        var item = await reader.GetByIdAsync(intId);
        if (item is null)
            return new NotFoundObjectResult(ApiResponse.Fail("Advance request not found."));

        return new OkObjectResult(ApiResponse.Ok(item));
    }

    // ── 新增（草稿）─────────────────────────────────────────────────────────

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var submittedById = await GetUserIdAsync(req);
        var body = await req.ReadFromJsonAsync<CreateAdvanceRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (body.Items.Length == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("At least one item is required."));

        if (!await db.Projects.AnyAsync(p => p.Id == body.ProjectId))
            throw AppException.NotFound("Project");

        // 指定審核者存在性驗證
        if (body.DesignatedReviewers is { Length: > 0 })
        {
            var reviewerIds = body.DesignatedReviewers.Select(r => r.ReviewerId).Distinct().ToList();
            var existCount = await db.Users.AsNoTracking().CountAsync(u => reviewerIds.Contains(u.Id));
            if (existCount != reviewerIds.Count)
                return new BadRequestObjectResult(ApiResponse.Fail("一或多位指定審核者不存在。"));
        }

        // 產生預支單號：ADV-yyyyMMdd-NNN（唯一索引保護並發）
        var today = Clock.Now;
        var prefix = $"ADV-{today:yyyyMMdd}-";
        var maxNo = await db.AdvanceRequests
            .Where(a => a.RequestNo.StartsWith(prefix))
            .MaxAsync(a => (string?)a.RequestNo);
        int seq = 1;
        if (maxNo is not null)
        {
            var seqStr = maxNo[prefix.Length..];
            if (int.TryParse(seqStr, out var parsed))
                seq = parsed + 1;
        }
        var requestNo = $"{prefix}{seq:D3}";

        var items = body.Items.Select((i, idx) => new AdvanceRequestItem
        {
            Category    = i.Category,
            SeqNo       = i.SeqNo,
            ItemName    = i.ItemName,
            UnitPrice   = i.UnitPrice,
            Quantity    = i.Quantity,
            TotalPrice  = i.TotalPrice,
            CashAmount  = i.CashAmount,
            CheckAmount = i.CheckAmount,
            Note        = i.Note,
            SortOrder   = i.SortOrder > 0 ? i.SortOrder : idx,
        }).ToList();

        var ar = new AdvanceRequest
        {
            RequestNo      = requestNo,
            ProjectId      = body.ProjectId,
            ActivityName   = body.ActivityName,
            ActivityPeriod = body.ActivityPeriod,
            AdvanceDate    = body.AdvanceDate,
            CashTotal      = items.Sum(i => i.CashAmount),
            CheckTotal     = items.Sum(i => i.CheckAmount),
            GrandTotal     = items.Sum(i => i.TotalPrice),
            SubmittedById  = submittedById,
            ApprovalStatus = "draft",
            CreatedAt      = today,
        };
        ar.Items = items;

        db.AdvanceRequests.Add(ar);
        await db.SaveChangesAsync();

        // 儲存指定審核者
        if (body.DesignatedReviewers is { Length: > 0 })
        {
            db.RequestDesignatedReviewers.AddRange(
                body.DesignatedReviewers.OrderBy(r => r.StepOrder).Select(r => new RequestDesignatedReviewer
                {
                    RequestType = "advance",
                    RequestId   = ar.Id,
                    ReviewerId  = r.ReviewerId,
                    StepOrder   = r.StepOrder,
                }));
            await db.SaveChangesAsync();
        }

        var dto = await reader.GetByIdAsync(ar.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Advance request created.")) { StatusCode = 201 };
    }

    // ── 更新草稿 ────────────────────────────────────────────────────────────

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advance request ID format."));

        var ar = await db.AdvanceRequests
                         .Include(x => x.Items)
                         .FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId)
            ?? throw AppException.NotFound("AdvanceRequest");

        if (ar.ApprovalStatus != "draft" && ar.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned advance requests can be edited.");

        var body = await req.ReadFromJsonAsync<UpdateAdvanceRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (body.ProjectId.HasValue)
        {
            if (!await db.Projects.AnyAsync(p => p.Id == body.ProjectId))
                throw AppException.NotFound("Project");
            ar.ProjectId = body.ProjectId.Value;
        }
        if (!string.IsNullOrEmpty(body.ActivityName))
            ar.ActivityName = body.ActivityName;
        if (!string.IsNullOrEmpty(body.ActivityPeriod))
            ar.ActivityPeriod = body.ActivityPeriod;
        if (body.AdvanceDate.HasValue)
            ar.AdvanceDate = body.AdvanceDate.Value;

        // 指定審核者整組替換（提供 DesignatedReviewers 時才更新）
        if (body.DesignatedReviewers is not null)
        {
            if (body.DesignatedReviewers.Length > 0)
            {
                var reviewerIds = body.DesignatedReviewers.Select(r => r.ReviewerId).Distinct().ToList();
                var existCount = await db.Users.AsNoTracking().CountAsync(u => reviewerIds.Contains(u.Id));
                if (existCount != reviewerIds.Count)
                    return new BadRequestObjectResult(ApiResponse.Fail("一或多位指定審核者不存在。"));
            }
            var old = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == "advance" && r.RequestId == intId)
                .ToListAsync();
            db.RequestDesignatedReviewers.RemoveRange(old);
            if (body.DesignatedReviewers.Length > 0)
            {
                db.RequestDesignatedReviewers.AddRange(
                    body.DesignatedReviewers.Select(r => new RequestDesignatedReviewer
                    {
                        RequestType = "advance",
                        RequestId   = intId,
                        ReviewerId  = r.ReviewerId,
                        StepOrder   = r.StepOrder,
                    }));
            }
        }

        if (body.Items is { Length: > 0 })
        {
            db.AdvanceRequestItems.RemoveRange(ar.Items);
            var newItems = body.Items.Select((i, idx) => new AdvanceRequestItem
            {
                AdvanceRequestId = ar.Id,
                Category    = i.Category,
                SeqNo       = i.SeqNo,
                ItemName    = i.ItemName,
                UnitPrice   = i.UnitPrice,
                Quantity    = i.Quantity,
                TotalPrice  = i.TotalPrice,
                CashAmount  = i.CashAmount,
                CheckAmount = i.CheckAmount,
                Note        = i.Note,
                SortOrder   = i.SortOrder > 0 ? i.SortOrder : idx,
            }).ToList();
            ar.Items      = newItems;
            ar.CashTotal  = newItems.Sum(i => i.CashAmount);
            ar.CheckTotal = newItems.Sum(i => i.CheckAmount);
            ar.GrandTotal = newItems.Sum(i => i.TotalPrice);
        }

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(ar.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Advance request updated."));
    }

    // ── 刪除草稿 ────────────────────────────────────────────────────────────

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advance request ID format."));

        var ar = await db.AdvanceRequests.FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId)
            ?? throw AppException.NotFound("AdvanceRequest");

        if (ar.ApprovalStatus != "draft")
            throw AppException.BadRequest("Only draft advance requests can be deleted.");

        db.AdvanceRequests.Remove(ar);
        await db.SaveChangesAsync();
        return new OkObjectResult(ApiResponse.Ok($"Advance request '{id}' deleted."));
    }

    // ── 送出申請 ────────────────────────────────────────────────────────────

    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advance request ID format."));

        var ar = await db.AdvanceRequests.FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId)
            ?? throw AppException.NotFound("AdvanceRequest");

        if (ar.ApprovalStatus != "draft" && ar.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned advance requests can be submitted.");

        // 退回重送：清除舊審核記錄，重置指定審核者狀態
        if (ar.ApprovalStatus == "returned")
        {
            var oldRecords = await db.ApprovalRecords
                .Where(r => r.ApplicationType == "advance" && r.ApplicationId == ar.Id)
                .ToListAsync();
            db.ApprovalRecords.RemoveRange(oldRecords);

            var oldOverrides = await db.EscalationOverrides
                .Where(o => o.ApplicationType == "advance" && o.ApplicationId == ar.Id)
                .ToListAsync();
            db.EscalationOverrides.RemoveRange(oldOverrides);

            // 重置指定審核者狀態為 pending
            var rdrsToReset = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == "advance" && r.RequestId == ar.Id)
                .ToListAsync();
            foreach (var rdr in rdrsToReset)
            {
                rdr.Status     = "pending";
                rdr.ReviewedAt = null;
                rdr.Comment    = null;
            }
        }

        // 自動關聯簽核流程
        if (ar.ApprovalItemId is null)
        {
            var flow = await db.ApprovalItems
                .AsNoTracking()
                .FirstOrDefaultAsync(ai => ai.ApplicationType == "advance" && ai.IsActive);
            if (flow is not null)
                ar.ApprovalItemId = flow.Id;
        }

        // Superadmin 直接自動核准
        var submitter = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (submitter?.IsSuperAdmin == true)
        {
            ar.ApprovalStatus   = "approved";
            ar.CurrentStepOrder = 1;
            ar.ReviewedAt       = Clock.Now;
            ar.ReviewedById     = userId;
            ar.ReviewNote       = "系統自動核准（Superadmin）";
            await db.SaveChangesAsync();
            var saDto = await reader.GetByIdAsync(ar.Id);
            return new OkObjectResult(ApiResponse.Ok(saDto, "Advance request auto-approved."));
        }

        // 若流程中有 UseApplicantDesignated 步驟，必須有指定審核者
        if (ar.ApprovalItemId.HasValue)
        {
            bool hasDesignatedStep = await db.ApprovalSteps.AsNoTracking()
                .AnyAsync(s => s.ApprovalItemId == ar.ApprovalItemId && s.UseApplicantDesignated);
            if (hasDesignatedStep)
            {
                bool hasReviewers = await db.RequestDesignatedReviewers
                    .AnyAsync(r => r.RequestType == "advance" && r.RequestId == ar.Id);
                if (!hasReviewers)
                    return new BadRequestObjectResult(ApiResponse.Fail("此簽核流程包含申請人指定審核步驟，請提供指定審核者。"));
            }
        }

        // 查詢指定審核者清單傳給 ResolveStartingStepAsync
        var designatedReviewers = await db.RequestDesignatedReviewers
            .AsNoTracking()
            .Where(r => r.RequestType == "advance" && r.RequestId == ar.Id)
            .OrderBy(r => r.StepOrder)
            .Select(r => new DesignatedReviewerRequest(r.ReviewerId, r.StepOrder))
            .ToListAsync();

        // 自審跳過邏輯（與請款一致，不升級）
        var (startStep, autoApproved, _) = await approvalFlow.ResolveStartingStepAsync(
            ar.ApprovalItemId, userId, "advance", designatedReviewers);

        if (autoApproved)
        {
            ar.ApprovalStatus   = "approved";
            ar.CurrentStepOrder = startStep;
            ar.ReviewedAt       = Clock.Now;
            ar.ReviewedById     = userId;
            ar.ReviewNote       = "系統自動核准（所有審核步驟皆為申請人本人）";
        }
        else
        {
            ar.ApprovalStatus   = "pending";
            ar.CurrentStepOrder = startStep;
        }

        await db.SaveChangesAsync();

        if (!autoApproved && ar.SubmittedById.HasValue)
        {
            bool isDesignatedStep = ar.ApprovalItemId.HasValue && await db.ApprovalSteps.AsNoTracking()
                .AnyAsync(s => s.ApprovalItemId == ar.ApprovalItemId
                    && s.StepOrder == startStep
                    && s.UseApplicantDesignated);
            if (isDesignatedStep)
            {
                var firstReviewer = await db.RequestDesignatedReviewers
                    .AsNoTracking()
                    .Where(r => r.RequestType == "advance" && r.RequestId == ar.Id && r.Status == "pending")
                    .OrderBy(r => r.StepOrder)
                    .FirstOrDefaultAsync();
                if (firstReviewer is not null)
                    await notifier.NotifySpecificReviewerAsync("advance", ar.Id, firstReviewer.ReviewerId, ar.SubmittedById.Value, false);
            }
            else
                await notifier.NotifyReviewersAsync("advance", ar.Id, ar.ApprovalItemId, startStep, ar.SubmittedById.Value);
        }

        var dto = await reader.GetByIdAsync(ar.Id);
        var msg = autoApproved ? "Advance request auto-approved." : "Advance request submitted.";
        return new OkObjectResult(ApiResponse.Ok(dto, msg));
    }

    // ── 更新撥款日（僅財務部/Superadmin）──────────────────────────────────────

    public async Task<IActionResult> UpdatePaymentDateAsync(HttpRequest req, string id)
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

        if (!user.IsSuperAdmin && user.Department?.Code != "FIN")
            return new ForbidResult();

        var ar = await db.AdvanceRequests.FindAsync(intId)
            ?? throw AppException.NotFound("AdvanceRequest");

        if (ar.ApprovalStatus != "approved")
            return new BadRequestObjectResult(ApiResponse.Fail("只有已核准的預支申請可以設定撥款日。"));

        var body = await req.ReadFromJsonAsync<UpdatePaymentDateRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (body.EstimatedPaymentDate.HasValue)
            ar.EstimatedPaymentDate = body.EstimatedPaymentDate.Value;
        if (body.PaidAt.HasValue)
            ar.PaidAt = body.PaidAt.Value;

        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok(new { ar.Id, ar.EstimatedPaymentDate, ar.PaidAt }, "撥款日期已更新。"));
    }

    // ── 退還差額匯款日期（僅財務部）──────────────────────────────────────────

    public async Task<IActionResult> RefundDateAsync(HttpRequest req, string id)
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

        if (!user.IsSuperAdmin && user.Department?.Code != "FIN")
            return new ForbidResult();

        var ar = await db.AdvanceRequests.FindAsync(intId)
            ?? throw AppException.NotFound("AdvanceRequest");

        if (!ar.IsClosed)
            return new BadRequestObjectResult(ApiResponse.Fail("只有已結案的預支申請可以設定匯款日期。"));

        if (ar.RefundAmount is null || ar.RefundAmount <= 0)
            return new BadRequestObjectResult(ApiResponse.Fail("此預支申請無需退還差額。"));

        var body = await req.ReadFromJsonAsync<RefundDateRequest>();
        if (body?.RefundedAt is null)
            return new BadRequestObjectResult(ApiResponse.Fail("請提供匯款日期。"));

        ar.RefundedAt = body.RefundedAt.Value;
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok(new { ar.Id, ar.RefundAmount, ar.RefundedAt }, "匯款日期已更新。"));
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private async Task<Guid> GetUserIdAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw AppException.Unauthorized("Invalid token claims.");
        return userId;
    }
}
