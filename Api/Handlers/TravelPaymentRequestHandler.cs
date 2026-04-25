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
/// 出差請款申請 Handler。
/// GET    /travel-payment-requests           → 列表（分頁）
/// POST   /travel-payment-requests           → 新增（EmployeeId 由 JWT 決定）
/// GET    /travel-payment-requests/{id}      → 單筆
/// PUT    /travel-payment-requests/{id}      → 更新（僅 draft/returned 才允許）
/// PATCH  /travel-payment-requests/{id}      → 部分更新（同上）
/// DELETE /travel-payment-requests/{id}      → 刪除（僅 draft 才允許）
/// PATCH  /travel-payment-requests/{id}/submit       → 送出（draft → pending）
/// PATCH  /travel-payment-requests/{id}/payment-date → 更新撥款日（僅財務部/Superadmin）
/// </summary>
public sealed class TravelPaymentRequestHandler(
    AppDbContext db,
    ITravelPaymentRequestReadService reader,
    IJwtService jwtService,
    IApprovalNotificationService notifier,
    IApprovalFlowService approvalFlow)
{
    private const string AppType = "travel_payment";

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
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel payment request ID format."));

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var exists = user?.IsSuperAdmin == true
            ? await db.TravelPaymentRequests.AnyAsync(x => x.Id == intId)
            : await db.TravelPaymentRequests.AnyAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (!exists)
            return new NotFoundObjectResult(ApiResponse.Fail("Travel payment request not found."));

        var item = await reader.GetByIdAsync(intId);
        return new OkObjectResult(ApiResponse.Ok(item));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        // EmployeeId 由 JWT 中的 sub claim 決定，不信任客戶端傳入的值
        var employeeId = await GetUserIdAsync(req);

        var body = await req.ReadFromJsonAsync<CreateTravelPaymentRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (string.IsNullOrWhiteSpace(body.Destination))
            return new BadRequestObjectResult(ApiResponse.Fail("Destination is required."));

        if (body.StartDate == default || body.EndDate == default)
            return new BadRequestObjectResult(ApiResponse.Fail("StartDate and EndDate are required."));

        if (body.EndDate < body.StartDate)
            return new BadRequestObjectResult(ApiResponse.Fail("EndDate must be on or after StartDate."));

        if (body.Items is null || body.Items.Length == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("At least one item is required."));

        // 指定審核者存在性驗證
        if (body.DesignatedReviewers is { Length: > 0 })
        {
            var reviewerIds = body.DesignatedReviewers.Select(r => r.ReviewerId).Distinct().ToList();
            var existCount = await db.Users.AsNoTracking().CountAsync(u => reviewerIds.Contains(u.Id));
            if (existCount != reviewerIds.Count)
                return new BadRequestObjectResult(ApiResponse.Fail("一或多位指定審核者不存在。"));
        }

        var items = body.Items.Select((i, idx) => new TravelPaymentRequestItem
        {
            Category   = i.Category,
            SeqNo      = i.SeqNo,
            ItemName   = i.ItemName,
            UnitPrice  = i.UnitPrice,
            Quantity   = i.Quantity,
            TotalPrice = i.TotalPrice,
            Note       = i.Note,
            SortOrder  = i.SortOrder > 0 ? i.SortOrder : idx,
        }).ToList();

        var request = new TravelPaymentRequest
        {
            EmployeeId      = employeeId,
            ApprovalItemId  = body.ApprovalItemId,
            Destination     = body.Destination,
            StartDate       = body.StartDate,
            EndDate         = body.EndDate,
            GrandTotal      = items.Sum(i => i.TotalPrice),
            Purpose         = body.Purpose,
            ProjectId       = body.ProjectId,
            ApprovalStatus  = "draft",
            CreatedAt       = Clock.Now,
            Items           = items,
        };
        db.TravelPaymentRequests.Add(request);
        await db.SaveChangesAsync();

        // 儲存指定審核者
        if (body.DesignatedReviewers is { Length: > 0 })
        {
            db.RequestDesignatedReviewers.AddRange(
                body.DesignatedReviewers.OrderBy(r => r.StepOrder).Select(r => new RequestDesignatedReviewer
                {
                    RequestType = AppType,
                    RequestId   = request.Id,
                    ReviewerId  = r.ReviewerId,
                    StepOrder   = r.StepOrder,
                }));
            await db.SaveChangesAsync();
        }

        var dto = await reader.GetByIdAsync(request.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Travel payment request created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel payment request ID format."));

        var body = await req.ReadFromJsonAsync<UpdateTravelPaymentRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.TravelPaymentRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.TravelPaymentRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("TravelPaymentRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned travel payment requests can be edited.");

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
                .Where(r => r.RequestType == AppType && r.RequestId == intId)
                .ToListAsync();
            db.RequestDesignatedReviewers.RemoveRange(old);
            if (body.DesignatedReviewers.Length > 0)
            {
                db.RequestDesignatedReviewers.AddRange(
                    body.DesignatedReviewers.Select(r => new RequestDesignatedReviewer
                    {
                        RequestType = AppType,
                        RequestId   = intId,
                        ReviewerId  = r.ReviewerId,
                        StepOrder   = r.StepOrder,
                    }));
            }
        }

        if (body.Destination is not null) item.Destination = body.Destination;
        if (body.StartDate.HasValue)      item.StartDate   = body.StartDate.Value;
        if (body.EndDate.HasValue)        item.EndDate     = body.EndDate.Value;
        if (body.Purpose is not null)     item.Purpose     = body.Purpose;
        if (body.ProjectId.HasValue)      item.ProjectId   = body.ProjectId == 0 ? null : body.ProjectId;

        // 明細項目整組替換（提供 Items 時才更新）
        if (body.Items is { Length: > 0 })
        {
            db.TravelPaymentRequestItems.RemoveRange(item.Items);
            var newItems = body.Items.Select((i, idx) => new TravelPaymentRequestItem
            {
                TravelPaymentRequestId = intId,
                Category   = i.Category,
                SeqNo      = i.SeqNo,
                ItemName   = i.ItemName,
                UnitPrice  = i.UnitPrice,
                Quantity   = i.Quantity,
                TotalPrice = i.TotalPrice,
                Note       = i.Note,
                SortOrder  = i.SortOrder > 0 ? i.SortOrder : idx,
            }).ToList();
            item.Items      = newItems;
            item.GrandTotal = newItems.Sum(i => i.TotalPrice);
        }

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(item.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Travel payment request updated."));
    }

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel payment request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.TravelPaymentRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.TravelPaymentRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("TravelPaymentRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned travel payment requests can be deleted.");

        db.TravelPaymentRequests.Remove(item);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Travel payment request '{id}' deleted."));
    }

    /// <summary>送出申請（draft → pending）</summary>
    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel payment request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.TravelPaymentRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.TravelPaymentRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("TravelPaymentRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned travel payment requests can be submitted.");

        var hasItems = await db.TravelPaymentRequestItems.AnyAsync(i => i.TravelPaymentRequestId == intId);
        if (!hasItems)
            return new BadRequestObjectResult(ApiResponse.Fail("出差請款申請至少需要一筆費用明細項目。"));

        // 退回重送時清除舊審核記錄，重置指定審核者狀態，重新走流程
        if (item.ApprovalStatus == "returned")
        {
            var oldRecords = await db.ApprovalRecords
                .Where(r => r.ApplicationType == AppType && r.ApplicationId == item.Id)
                .ToListAsync();
            db.ApprovalRecords.RemoveRange(oldRecords);

            var oldOverrides = await db.EscalationOverrides
                .Where(o => o.ApplicationType == AppType && o.ApplicationId == item.Id)
                .ToListAsync();
            db.EscalationOverrides.RemoveRange(oldOverrides);

            // 重置指定審核者狀態為 pending
            var rdrsToReset = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == AppType && r.RequestId == item.Id)
                .ToListAsync();
            foreach (var rdr in rdrsToReset)
            {
                rdr.Status     = "pending";
                rdr.ReviewedAt = null;
                rdr.Comment    = null;
            }
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
            return new OkObjectResult(ApiResponse.Ok(saDto, "Travel payment request auto-approved."));
        }

        // 自動關聯簽核流程（依 ApplicationType 查找啟用的流程）
        if (item.ApprovalItemId is null)
        {
            var flow = await db.ApprovalItems
                .AsNoTracking()
                .FirstOrDefaultAsync(ai => ai.ApplicationType == AppType && ai.IsActive);
            if (flow is not null)
                item.ApprovalItemId = flow.Id;
        }

        // 若流程中有 UseApplicantDesignated 步驟，必須有指定審核者
        if (item.ApprovalItemId.HasValue)
        {
            bool hasDesignatedStep = await db.ApprovalSteps.AsNoTracking()
                .AnyAsync(s => s.ApprovalItemId == item.ApprovalItemId && s.UseApplicantDesignated);
            if (hasDesignatedStep)
            {
                bool hasReviewers = await db.RequestDesignatedReviewers
                    .AnyAsync(r => r.RequestType == AppType && r.RequestId == item.Id);
                if (!hasReviewers)
                    return new BadRequestObjectResult(ApiResponse.Fail("此簽核流程包含申請人指定審核步驟，請提供指定審核者。"));
            }
        }

        var designatedReviewers = await db.RequestDesignatedReviewers
            .AsNoTracking()
            .Where(r => r.RequestType == AppType && r.RequestId == item.Id)
            .OrderBy(r => r.StepOrder)
            .Select(r => new DesignatedReviewerRequest(r.ReviewerId, r.StepOrder))
            .ToListAsync();

        var (startStep, autoApproved, escalation) =
            await approvalFlow.ResolveStartingStepAsync(item.ApprovalItemId, userId, AppType, designatedReviewers);

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

        if (escalation is not null)
        {
            db.EscalationOverrides.Add(new EscalationOverride
            {
                ApplicationType  = AppType,
                ApplicationId    = item.Id,
                StepOrder        = startStep,
                ReviewerId       = escalation.ReviewerId,
                OnBehalfOfUserId = escalation.OnBehalfOfUserId,
                CreatedAt        = Clock.Now,
            });
        }

        await db.SaveChangesAsync();

        if (!autoApproved)
        {
            if (escalation is not null)
                await notifier.NotifySpecificReviewerAsync(AppType, item.Id, escalation.ReviewerId, userId, escalation.OnBehalfOfUserId is not null);
            else
            {
                bool isDesignatedStep = item.ApprovalItemId.HasValue && await db.ApprovalSteps.AsNoTracking()
                    .AnyAsync(s => s.ApprovalItemId == item.ApprovalItemId
                        && s.StepOrder == startStep
                        && s.UseApplicantDesignated);
                if (isDesignatedStep)
                {
                    var firstReviewer = await db.RequestDesignatedReviewers
                        .AsNoTracking()
                        .Where(r => r.RequestType == AppType && r.RequestId == item.Id && r.Status == "pending")
                        .OrderBy(r => r.StepOrder)
                        .FirstOrDefaultAsync();
                    if (firstReviewer is not null)
                        await notifier.NotifySpecificReviewerAsync(AppType, item.Id, firstReviewer.ReviewerId, userId, false);
                }
                else
                    await notifier.NotifyReviewersAsync(AppType, item.Id, item.ApprovalItemId, startStep, userId);
            }
        }

        var dto = await reader.GetByIdAsync(item.Id);
        var msg = autoApproved ? "Travel payment request auto-approved." : "Travel payment request submitted.";
        return new OkObjectResult(ApiResponse.Ok(dto, msg));
    }

    /// <summary>更新撥款日（僅財務部/Superadmin）</summary>
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

        if (!user.IsSuperAdmin && !DepartmentCodes.FinancialAndAbove.Contains(user.Department?.Code ?? ""))
            throw AppException.Forbidden("僅財務體系部門或 Superadmin 可更新撥款日。");

        var tpr = await db.TravelPaymentRequests.FindAsync(intId)
            ?? throw AppException.NotFound("TravelPaymentRequest");

        if (tpr.ApprovalStatus != "approved")
            return new BadRequestObjectResult(ApiResponse.Fail("只有已核准的出差請款申請可以設定撥款日。"));

        var body = await req.ReadFromJsonAsync<UpdatePaymentDateRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (body.EstimatedPaymentDate.HasValue)
            tpr.EstimatedPaymentDate = body.EstimatedPaymentDate.Value;
        if (body.PaidAt.HasValue)
        {
            tpr.PaidAt        = body.PaidAt.Value;
            tpr.PaidByUserId  = userId;
        }

        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok(
            new { tpr.Id, tpr.EstimatedPaymentDate, tpr.PaidAt },
            "撥款日期已更新。"));
    }

    // ── Helper ──────────────────────────────────────────────────────────────────

    private async Task<Guid> GetUserIdAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw AppException.Unauthorized("Invalid token claims.");
        return userId;
    }
}
