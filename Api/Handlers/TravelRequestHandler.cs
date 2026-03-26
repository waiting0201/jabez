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
/// GET    /travel-requests           → 列表（分頁）
/// POST   /travel-requests           → 新增（EmployeeId 由 JWT 決定）
/// GET    /travel-requests/{id}      → 單筆
/// PUT    /travel-requests/{id}      → 更新（僅 draft/returned 才允許）
/// DELETE /travel-requests/{id}      → 刪除（僅 draft 才允許）
/// PATCH  /travel-requests/{id}/submit → 送出（draft → pending）
/// </summary>
public sealed class TravelRequestHandler(
    AppDbContext db,
    ITravelRequestReadService reader,
    IJwtService jwtService,
    IApprovalNotificationService notifier,
    IApprovalFlowService approvalFlow,
    ICalendarDayReadService calendarDayReader)
{
    public async Task<IActionResult> GetAllAsync(HttpRequest req, bool isHolidayTravel = false)
    {
        var userId = await GetUserIdAsync(req);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        Guid? filterUserId = user?.IsSuperAdmin == true ? null : userId;
        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        var result = await reader.GetPagedAsync(page, pageSize, filterUserId, isHolidayTravel);
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    public async Task<IActionResult> GetByIdAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel request ID format."));

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var exists = user?.IsSuperAdmin == true
            ? await db.TravelRequests.AnyAsync(x => x.Id == intId)
            : await db.TravelRequests.AnyAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (!exists)
            return new NotFoundObjectResult(ApiResponse.Fail("Travel request not found."));

        var item = await reader.GetByIdAsync(intId);
        return new OkObjectResult(ApiResponse.Ok(item));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req, bool isHolidayTravel = false)
    {
        var appType = isHolidayTravel ? "holiday_travel" : "travel";
        // BUG-04: EmployeeId 由 JWT 中的 sub claim 決定，不信任客戶端傳入的值
        var employeeId = await GetUserIdAsync(req);

        var body = await req.ReadFromJsonAsync<CreateTravelRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (string.IsNullOrWhiteSpace(body.Destination))
            return new BadRequestObjectResult(ApiResponse.Fail("Destination is required."));

        if (body.StartDate == default || body.EndDate == default)
            return new BadRequestObjectResult(ApiResponse.Fail("StartDate and EndDate are required."));

        if (body.EndDate < body.StartDate)
            return new BadRequestObjectResult(ApiResponse.Fail("EndDate must be on or after StartDate."));

        // 明細項目驗證：至少需要一筆
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

        // 假日出差：參與者存在性驗證
        if (isHolidayTravel && body.Participants is { Length: > 0 })
        {
            var participantIds = body.Participants.Select(p => p.UserId).Distinct().ToList();
            var existCount = await db.Users.AsNoTracking().CountAsync(u => participantIds.Contains(u.Id));
            if (existCount != participantIds.Count)
                return new BadRequestObjectResult(ApiResponse.Fail("一或多位出差參與者不存在。"));
        }

        // 建立明細項目，計算 GrandTotal
        var items = body.Items.Select((i, idx) => new TravelRequestItem
        {
            Category  = i.Category,
            SeqNo     = i.SeqNo,
            ItemName  = i.ItemName,
            UnitPrice = i.UnitPrice,
            Quantity  = i.Quantity,
            TotalPrice = i.TotalPrice,
            Note      = i.Note,
            SortOrder = i.SortOrder > 0 ? i.SortOrder : idx,
        }).ToList();

        var travelRequest = new TravelRequest
        {
            EmployeeId      = employeeId,   // 強制使用 JWT 身分，忽略 body.EmployeeId
            ApprovalItemId  = body.ApprovalItemId,
            Destination     = body.Destination,
            StartDate       = body.StartDate,
            EndDate         = body.EndDate,
            GrandTotal      = items.Sum(i => i.TotalPrice),
            Purpose         = body.Purpose,
            ProjectId       = body.ProjectId,
            IsHolidayTravel = isHolidayTravel,  // 強制由路由決定，忽略 body 的值
            ApprovalStatus  = "draft",
            CreatedAt       = Clock.Now,
            Items           = items,
        };
        db.TravelRequests.Add(travelRequest);
        await db.SaveChangesAsync();

        // 儲存指定審核者
        if (body.DesignatedReviewers is { Length: > 0 })
        {
            db.RequestDesignatedReviewers.AddRange(
                body.DesignatedReviewers.OrderBy(r => r.StepOrder).Select(r => new RequestDesignatedReviewer
                {
                    RequestType = appType,
                    RequestId   = travelRequest.Id,
                    ReviewerId  = r.ReviewerId,
                    StepOrder   = r.StepOrder,
                }));
            await db.SaveChangesAsync();
        }

        // 假日出差：儲存參與者
        if (isHolidayTravel && body.Participants is { Length: > 0 })
        {
            db.TravelRequestParticipants.AddRange(
                body.Participants.Select(p => new TravelRequestParticipant
                {
                    TravelRequestId = travelRequest.Id,
                    UserId          = p.UserId,
                    SortOrder       = p.SortOrder,
                }));
            await db.SaveChangesAsync();
        }

        var dto = await reader.GetByIdAsync(travelRequest.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Travel request created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id, bool isHolidayTravel = false)
    {
        var appType = isHolidayTravel ? "holiday_travel" : "travel";
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel request ID format."));

        var body = await req.ReadFromJsonAsync<UpdateTravelRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.TravelRequests.Include(x => x.Items).Include(x => x.Participants).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.TravelRequests.Include(x => x.Items).Include(x => x.Participants).FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("TravelRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned travel requests can be edited.");

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
                .Where(r => r.RequestType == appType && r.RequestId == intId)
                .ToListAsync();
            db.RequestDesignatedReviewers.RemoveRange(old);
            if (body.DesignatedReviewers.Length > 0)
            {
                db.RequestDesignatedReviewers.AddRange(
                    body.DesignatedReviewers.Select(r => new RequestDesignatedReviewer
                    {
                        RequestType = appType,
                        RequestId   = intId,
                        ReviewerId  = r.ReviewerId,
                        StepOrder   = r.StepOrder,
                    }));
            }
        }

        // 假日出差：參與者整組替換（提供 Participants 時才更新）
        if (isHolidayTravel && body.Participants is not null)
        {
            if (body.Participants.Length > 0)
            {
                var participantIds = body.Participants.Select(p => p.UserId).Distinct().ToList();
                var existCount = await db.Users.AsNoTracking().CountAsync(u => participantIds.Contains(u.Id));
                if (existCount != participantIds.Count)
                    return new BadRequestObjectResult(ApiResponse.Fail("一或多位出差參與者不存在。"));
            }
            db.TravelRequestParticipants.RemoveRange(item.Participants);
            if (body.Participants.Length > 0)
            {
                db.TravelRequestParticipants.AddRange(
                    body.Participants.Select(p => new TravelRequestParticipant
                    {
                        TravelRequestId = intId,
                        UserId          = p.UserId,
                        SortOrder       = p.SortOrder,
                    }));
            }
        }

        if (body.Destination is not null)  item.Destination = body.Destination;
        if (body.StartDate.HasValue)       item.StartDate   = body.StartDate.Value;
        if (body.EndDate.HasValue)         item.EndDate     = body.EndDate.Value;
        if (body.Purpose is not null)      item.Purpose     = body.Purpose;
        if (body.ProjectId.HasValue)       item.ProjectId   = body.ProjectId == 0 ? null : body.ProjectId;
        // IsHolidayTravel 不允許透過 UpdateAsync 修改（由路由決定），忽略 body.IsHolidayTravel

        // 明細項目整組替換（提供 Items 時才更新）
        if (body.Items is { Length: > 0 })
        {
            db.TravelRequestItems.RemoveRange(item.Items);
            var newItems = body.Items.Select((i, idx) => new TravelRequestItem
            {
                TravelRequestId = intId,
                Category  = i.Category,
                SeqNo     = i.SeqNo,
                ItemName  = i.ItemName,
                UnitPrice = i.UnitPrice,
                Quantity  = i.Quantity,
                TotalPrice = i.TotalPrice,
                Note      = i.Note,
                SortOrder = i.SortOrder > 0 ? i.SortOrder : idx,
            }).ToList();
            item.Items      = newItems;
            item.GrandTotal = newItems.Sum(i => i.TotalPrice);
        }

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(item.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Travel request updated."));
    }

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.TravelRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.TravelRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("TravelRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned travel requests can be deleted.");

        db.TravelRequests.Remove(item);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Travel request '{id}' deleted."));
    }

    /// <summary>送出申請（draft → pending）</summary>
    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id, bool isHolidayTravel = false)
    {
        // 根據路由決定申請類型（假日出差 vs 一般出差）
        var appType = isHolidayTravel ? "holiday_travel" : "travel";

        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.TravelRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.TravelRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("TravelRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned travel requests can be submitted.");

        // 送出前確認有明細項目
        var hasItems = await db.TravelRequestItems.AnyAsync(i => i.TravelRequestId == intId);
        if (!hasItems)
            return new BadRequestObjectResult(ApiResponse.Fail("出差申請至少需要一筆費用明細項目。"));

        // 假日出差：計算 HolidayDays（送出時計算，需確認行事曆資料已匯入）
        if (isHolidayTravel)
        {
            var hasCalendarData = await calendarDayReader.HasDataForRangeAsync(item.StartDate, item.EndDate);
            if (!hasCalendarData)
                return new BadRequestObjectResult(ApiResponse.Fail($"行事曆資料尚未匯入（{item.StartDate:yyyy/MM/dd} ~ {item.EndDate:yyyy/MM/dd}），請聯絡管理員匯入後再送出。"));

            item.HolidayDays = await calendarDayReader.CountHolidaysAsync(item.StartDate, item.EndDate);
        }

        // 退回重送時清除舊審核記錄，重置指定審核者狀態，重新走流程
        if (item.ApprovalStatus == "returned")
        {
            var oldRecords = await db.ApprovalRecords
                .Where(r => r.ApplicationType == appType && r.ApplicationId == item.Id)
                .ToListAsync();
            db.ApprovalRecords.RemoveRange(oldRecords);

            var oldOverrides = await db.EscalationOverrides
                .Where(o => o.ApplicationType == appType && o.ApplicationId == item.Id)
                .ToListAsync();
            db.EscalationOverrides.RemoveRange(oldOverrides);

            // 重置指定審核者狀態為 pending
            var rdrsToReset = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == appType && r.RequestId == item.Id)
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
            return new OkObjectResult(ApiResponse.Ok(saDto, "Travel request auto-approved."));
        }

        // 自動關聯簽核流程（依 ApplicationType 查找啟用的流程）
        if (item.ApprovalItemId is null)
        {
            var flow = await db.ApprovalItems
                .AsNoTracking()
                .FirstOrDefaultAsync(ai => ai.ApplicationType == appType && ai.IsActive);
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
                    .AnyAsync(r => r.RequestType == appType && r.RequestId == item.Id);
                if (!hasReviewers)
                    return new BadRequestObjectResult(ApiResponse.Fail("此簽核流程包含申請人指定審核步驟，請提供指定審核者。"));
            }
        }

        // 查詢指定審核者清單傳給 ResolveStartingStepAsync
        var designatedReviewers = await db.RequestDesignatedReviewers
            .AsNoTracking()
            .Where(r => r.RequestType == appType && r.RequestId == item.Id)
            .OrderBy(r => r.StepOrder)
            .Select(r => new DesignatedReviewerRequest(r.ReviewerId, r.StepOrder))
            .ToListAsync();

        // 解析審核步驟（含升級審核邏輯）
        var (startStep, autoApproved, escalation) =
            await approvalFlow.ResolveStartingStepAsync(item.ApprovalItemId, userId, appType, designatedReviewers);

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
                ApplicationType  = appType,
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
                await notifier.NotifySpecificReviewerAsync(appType, item.Id, escalation.ReviewerId, userId, escalation.OnBehalfOfUserId is not null);
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
                        .Where(r => r.RequestType == appType && r.RequestId == item.Id && r.Status == "pending")
                        .OrderBy(r => r.StepOrder)
                        .FirstOrDefaultAsync();
                    if (firstReviewer is not null)
                        await notifier.NotifySpecificReviewerAsync(appType, item.Id, firstReviewer.ReviewerId, userId, false);
                }
                else
                    await notifier.NotifyReviewersAsync(appType, item.Id, item.ApprovalItemId, startStep, userId);
            }
        }

        var dto = await reader.GetByIdAsync(item.Id);
        var msg = autoApproved ? "Travel request auto-approved." : "Travel request submitted.";
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

        var tr = await db.TravelRequests.FindAsync(intId)
            ?? throw AppException.NotFound("TravelRequest");

        if (tr.ApprovalStatus != "approved")
            return new BadRequestObjectResult(ApiResponse.Fail("只有已核准的出差申請可以設定撥款日。"));

        var body = await req.ReadFromJsonAsync<UpdatePaymentDateRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (body.EstimatedPaymentDate.HasValue)
            tr.EstimatedPaymentDate = body.EstimatedPaymentDate.Value;
        if (body.PaidAt.HasValue)
        {
            tr.PaidAt = body.PaidAt.Value;
            tr.PaidByUserId = userId;
        }
        if (body.EstimatedRefundDate.HasValue)
            tr.EstimatedRefundDate = body.EstimatedRefundDate.Value;
        if (body.RefundedAt.HasValue)
        {
            tr.RefundedAt = body.RefundedAt.Value;
            tr.RefundedByUserId = userId;
        }

        await db.SaveChangesAsync();

        var msg = (body.EstimatedRefundDate.HasValue || body.RefundedAt.HasValue) ? "退款日期已更新。" : "撥款日期已更新。";
        return new OkObjectResult(ApiResponse.Ok(new { tr.Id, tr.EstimatedPaymentDate, tr.PaidAt, tr.EstimatedRefundDate, tr.RefundedAt }, msg));
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
