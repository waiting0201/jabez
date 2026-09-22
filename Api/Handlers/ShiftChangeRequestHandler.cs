using System.IdentityModel.Tokens.Jwt;
using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Handlers;

/// <summary>
/// 改班申請（四週彈性工時 §3.5.2）—— 開放期（10–25 日）結束、班表定案鎖定後的異動途徑。
///
/// GET    /shift-changes                       清單
/// GET    /shift-changes/changeable-dates      可申請改班的日期（逐日現況 + 配額）
/// POST   /shift-changes                       新增草稿
/// GET    /shift-changes/{id}                  詳情
/// PUT|PATCH /shift-changes/{id}               修改草稿
/// PATCH  /shift-changes/{id}/submit           送簽
/// DELETE /shift-changes/{id}                  刪除草稿
///
/// ⚠ **與銷假申請的關鍵差異**：銷假借用請假的流程設定（`ResolveApprovalItemIdAsync("leave", …)`），
/// 改班則以**自己的 ApplicationType** 解析（§3.5.2 的逐部門六條簽核路線，
/// 管理員要在〈簽核流程設定〉建得出 6 個 ApprovalItem）。
///
/// ⚠ **核准後才寫入班表**：未核准前 `ShiftScheduleDay` 完全不動，故退回 / 拒絕都不需要任何回滾。
/// </summary>
public sealed class ShiftChangeRequestHandler(
    AppDbContext db,
    IShiftChangeRequestReadService reader,
    IJwtService jwtService,
    IApprovalNotificationService notifier,
    IApprovalFlowService approvalFlow,
    ICalendarDayReadService calendarReader)
{
    private const string AppType = ShiftChangeRequestService.AppType;

    // ── Read ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        int page     = int.TryParse(req.Query["page"], out var p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;

        // 只看自己的單（簽核者從〈簽核作業〉進入，走 ApprovalTaskHandler）
        var result = await reader.GetPagedAsync(page, pageSize, userId);
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    public async Task<IActionResult> GetByIdAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid shift change ID format."));

        var dto = await reader.GetByIdAsync(intId) ?? throw AppException.NotFound("ShiftChangeRequest");

        // 檢視授權沿用全站單一真相：申請人本人 ∪ Superadmin ∪ 持 approval-tasks:read ∪ 曾審核 ∪ 指定審核 ∪ 升級指派。
        // 不符一律回 404（不透露單據是否存在）。
        var principal = await jwtService.ValidateRequestAsync(req);
        bool canView = await RequestViewAccess.CanViewAsync(
            db, principal, userId, AppType, intId, isApplicant: dto.EmployeeId == userId);
        if (!canView) throw AppException.NotFound("ShiftChangeRequest");

        return new OkObjectResult(ApiResponse.Ok(dto));
    }

    /// <summary>
    /// 可申請改班的日期清單（逐日現況 + 目前配額）。
    /// 已排除：國定假日（唯讀）、今天以前的日期。
    /// </summary>
    public async Task<IActionResult> GetChangeableDatesAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var now = Clock.Now;
        int year  = int.TryParse(req.Query["year"],  out var y) ? y : now.AddMonths(1).Year;
        int month = int.TryParse(req.Query["month"], out var m) ? m : now.AddMonths(1).Month;
        if (month is < 1 or > 12) throw AppException.BadRequest("月份必須介於 1 ~ 12。");

        var monthStart = new DateTime(year, month, 1);
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);

        var holidays = await ShiftScheduleMap.LoadPublicHolidaysAsync(calendarReader, monthStart, monthEnd);
        var saved = await db.ShiftScheduleDays.AsNoTracking()
            .Where(d => d.UserId == userId && d.Date >= monthStart && d.Date <= monthEnd)
            .ToDictionaryAsync(d => d.Date.Date, d => d.DayType);

        var map = ShiftScheduleMap.BuildMonthDayTypes(monthStart, monthEnd, saved, holidays);

        var monthStatus = await db.ShiftScheduleMonths.AsNoTracking()
            .Where(x => x.UserId == userId && x.Year == year && x.Month == month)
            .Select(x => x.Status)
            .FirstOrDefaultAsync();

        var dates = new List<ShiftChangeDateDto>();
        foreach (var (date, type) in map.OrderBy(kv => kv.Key))
        {
            if (date < now.Date) continue;                       // 過去的日子改不了
            bool isHoliday = holidays.ContainsKey(date);
            dates.Add(new ShiftChangeDateDto(date, type, type, isHoliday));
        }

        int off  = map.Count(kv => kv.Value == WorkDayTypes.StatutoryOff);
        int rest = map.Count(kv => kv.Value == WorkDayTypes.RestDay);

        return new OkObjectResult(ApiResponse.Ok(new ChangeableShiftDatesDto(
            year, month, monthStatus, [.. dates],
            off, rest,
            ShiftScheduleValidator.RequiredStatutoryOffDays,
            ShiftScheduleValidator.RequiredRestDaysFor(year, month))));
    }

    // ── Write ────────────────────────────────────────────────────────────────

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var body = await req.ReadFromJsonAsync<CreateShiftChangeRequest>()
                   ?? throw AppException.BadRequest("Invalid request body.");

        if (body.Month is < 1 or > 12) throw AppException.BadRequest("月份必須介於 1 ~ 12。");
        RequestDateGuard.Ensure(new DateTime(body.Year, body.Month, 1), "改班月份");

        var entity = new ShiftChangeRequest
        {
            EmployeeId = userId,
            Year       = body.Year,
            Month      = body.Month,
            Reason     = (body.Reason ?? "").Trim(),
            CreatedAt  = Clock.Now,
        };
        db.ShiftChangeRequests.Add(entity);
        await db.SaveChangesAsync();

        await ReplaceDatesAsync(entity, body.Dates ?? []);
        if (body.DesignatedReviewers is { Length: > 0 })
        {
            db.RequestDesignatedReviewers.AddRange(
                DesignatedReviewerHelper.BuildEntities(AppType, entity.Id, body.DesignatedReviewers));
        }
        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(entity.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Shift change request created."));
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid shift change ID format."));

        var entity = await LoadOwnedAsync(userId, intId) ?? throw AppException.NotFound("ShiftChangeRequest");
        if (entity.ApprovalStatus is not ("draft" or "returned"))
            throw AppException.BadRequest("Only draft or returned shift change requests can be updated.");

        var body = await req.ReadFromJsonAsync<UpdateShiftChangeRequest>()
                   ?? throw AppException.BadRequest("Invalid request body.");

        if (body.Reason is not null) entity.Reason = body.Reason.Trim();
        if (body.Dates is not null)  await ReplaceDatesAsync(entity, body.Dates);
        if (body.DesignatedReviewers is not null)
        {
            db.RequestDesignatedReviewers.RemoveRange(
                await db.RequestDesignatedReviewers
                    .Where(r => r.RequestType == AppType && r.RequestId == entity.Id).ToListAsync());
            if (body.DesignatedReviewers.Length > 0)
            {
                db.RequestDesignatedReviewers.AddRange(
                    DesignatedReviewerHelper.BuildEntities(AppType, entity.Id, body.DesignatedReviewers));
            }
        }

        await db.SaveChangesAsync();
        var dto = await reader.GetByIdAsync(entity.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Shift change request updated."));
    }

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid shift change ID format."));

        var entity = await LoadOwnedAsync(userId, intId) ?? throw AppException.NotFound("ShiftChangeRequest");
        if (entity.ApprovalStatus is not ("draft" or "returned"))
            throw AppException.BadRequest("Only draft or returned shift change requests can be deleted.");

        // 多型關聯無 FK，必須手動清 —— 殘列會掛著 ReviewerId 擋住日後刪使用者
        db.ApprovalRecords.RemoveRange(
            await db.ApprovalRecords.Where(r => r.ApplicationType == AppType && r.ApplicationId == intId).ToListAsync());
        db.EscalationOverrides.RemoveRange(
            await db.EscalationOverrides.Where(o => o.ApplicationType == AppType && o.ApplicationId == intId).ToListAsync());
        db.RequestDesignatedReviewers.RemoveRange(
            await db.RequestDesignatedReviewers.Where(r => r.RequestType == AppType && r.RequestId == intId).ToListAsync());

        db.ShiftChangeRequests.Remove(entity);
        await db.SaveChangesAsync();
        return new OkObjectResult(ApiResponse.Ok<object?>(null, "Shift change request deleted."));
    }

    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid shift change ID format."));

        var entity = await LoadOwnedAsync(userId, intId) ?? throw AppException.NotFound("ShiftChangeRequest");
        if (entity.ApprovalStatus is not ("draft" or "returned"))
            throw AppException.BadRequest("Only draft or returned shift change requests can be submitted.");

        var dateCount = await db.ShiftChangeRequestDates.CountAsync(d => d.ShiftChangeRequestId == entity.Id);
        if (dateCount == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("請至少選擇一天要調整的日期。"));
        if (string.IsNullOrWhiteSpace(entity.Reason))
            return new BadRequestObjectResult(ApiResponse.Fail("請填寫改班原因。"));

        // 送簽時才取號；退回重送不改號（單號日期＝首次送簽日）
        if (string.IsNullOrEmpty(entity.RequestNo))
            entity.RequestNo = await RequestNoGenerator.NextAsync(
                db.ShiftChangeRequests.Select(x => x.RequestNo), ShiftChangeRequestService.RequestNoPrefix, Clock.Now);

        entity.SubmittedAt ??= Clock.Now;

        // 退回重送：清除舊審核足跡、重置指定審核者
        if (entity.ApprovalStatus == "returned")
        {
            db.ApprovalRecords.RemoveRange(
                await db.ApprovalRecords.Where(r => r.ApplicationType == AppType && r.ApplicationId == entity.Id).ToListAsync());
            db.EscalationOverrides.RemoveRange(
                await db.EscalationOverrides.Where(o => o.ApplicationType == AppType && o.ApplicationId == entity.Id).ToListAsync());

            foreach (var rdr in await db.RequestDesignatedReviewers
                         .Where(r => r.RequestType == AppType && r.RequestId == entity.Id).ToListAsync())
            {
                rdr.Status     = "pending";
                rdr.ReviewedAt = null;
                rdr.Comment    = null;
            }
        }

        var submitter = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (submitter?.IsSuperAdmin == true)
        {
            entity.ApprovalStatus   = "approved";
            entity.CurrentStepOrder = 1;
            entity.ReviewedAt       = Clock.Now;
            entity.ReviewedById     = userId;
            entity.ReviewNote       = "系統自動核准（Superadmin）";
            await ShiftChangeRequestService.ApplyAsync(db, entity);
            await db.SaveChangesAsync();
            return new OkObjectResult(ApiResponse.Ok(await reader.GetByIdAsync(entity.Id), "Shift change auto-approved."));
        }

        // ⚠ 以**自己的** ApplicationType 解析流程（不是借用 leave）——
        //    §3.5.2 的逐部門六條路線要靠這個對應到各自的 ApprovalItem
        entity.ApprovalItemId ??= await approvalFlow.ResolveApprovalItemIdAsync(AppType, submitter?.DepartmentId);

        await DesignatedReviewerHelper.ValidateAndNormalizeAsync(db, AppType, entity.Id, entity.ApprovalItemId, userId);
        await db.SaveChangesAsync();

        var designatedReviewers = await DesignatedReviewerHelper.ReadForFlowAsync(db, AppType, entity.Id);

        // 改班沒有天數門檻概念 → requestDays 傳 null，MinDays 關卡一律納入
        var (startStep, autoApproved, escalation) =
            await approvalFlow.ResolveStartingStepAsync(entity.ApprovalItemId, userId, AppType, designatedReviewers, requestDays: null);

        if (autoApproved)
        {
            entity.ApprovalStatus   = "approved";
            entity.CurrentStepOrder = startStep;
            entity.ReviewedAt       = Clock.Now;
            entity.ReviewedById     = userId;
            entity.ReviewNote       = "系統自動核准（所有審核步驟皆為申請人本人）";
            await ShiftChangeRequestService.ApplyAsync(db, entity);
        }
        else
        {
            entity.ApprovalStatus   = "pending";
            entity.CurrentStepOrder = startStep;
        }

        if (escalation is not null)
        {
            db.EscalationOverrides.Add(new EscalationOverride
            {
                ApplicationType  = AppType,
                ApplicationId    = entity.Id,
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
                await notifier.NotifySpecificReviewerAsync(AppType, entity.Id, escalation.ReviewerId, userId, escalation.OnBehalfOfUserId is not null);
            else
            {
                bool isDesignatedStep = designatedReviewers.Any(r => r.ApprovalStepOrder == startStep);
                if (isDesignatedStep)
                {
                    var firstReviewer = await db.RequestDesignatedReviewers.AsNoTracking()
                        .Where(r => r.RequestType == AppType && r.RequestId == entity.Id
                                 && r.ApprovalStepOrder == startStep && r.Status == "pending")
                        .OrderBy(r => r.StepOrder)
                        .FirstOrDefaultAsync();
                    if (firstReviewer is not null)
                        await notifier.NotifySpecificReviewerAsync(AppType, entity.Id, firstReviewer.ReviewerId, userId, false);
                }
                else
                    await notifier.NotifyReviewersAsync(AppType, entity.Id, entity.ApprovalItemId, startStep, userId);
            }
        }

        var dto = await reader.GetByIdAsync(entity.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, autoApproved ? "Shift change auto-approved." : "Shift change submitted."));
    }

    // ── 內部 ────────────────────────────────────────────────────────────────

    /// <summary>整批替換逐日明細，並把「送單當下的日別」快照進 FromDayType。</summary>
    private async Task ReplaceDatesAsync(ShiftChangeRequest entity, ShiftChangeDateRequest[] dates)
    {
        db.ShiftChangeRequestDates.RemoveRange(
            await db.ShiftChangeRequestDates.Where(d => d.ShiftChangeRequestId == entity.Id).ToListAsync());

        if (dates.Length == 0) return;

        var monthStart = new DateTime(entity.Year, entity.Month, 1);
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);

        var holidays = await ShiftScheduleMap.LoadPublicHolidaysAsync(calendarReader, monthStart, monthEnd);
        var saved = await db.ShiftScheduleDays.AsNoTracking()
            .Where(d => d.UserId == entity.EmployeeId && d.Date >= monthStart && d.Date <= monthEnd)
            .ToDictionaryAsync(d => d.Date.Date, d => d.DayType);
        var map = ShiftScheduleMap.BuildMonthDayTypes(monthStart, monthEnd, saved, holidays);

        foreach (var d in dates.DistinctBy(x => x.Date.Date))
        {
            var date = d.Date.Date;
            if (date < monthStart || date > monthEnd)
                throw AppException.BadRequest($"{date:M/d} 不在 {entity.Year}/{entity.Month} 範圍內。");
            if (holidays.ContainsKey(date))
                throw AppException.BadRequest($"{date:M/d} 為國定假日，不可申請變更。");
            if (!WorkDayTypes.IsSelectable(d.ToDayType))
                throw AppException.BadRequest($"{date:M/d} 的日別「{d.ToDayType}」不是可勾選的狀態。");

            var from = map.TryGetValue(date, out var t) ? t : WorkDayTypes.Work;
            if (from == d.ToDayType)
                throw AppException.BadRequest($"{date:M/d} 原本就是{WorkDayTypeNames.GetZh(from)}，不需申請變更。");

            db.ShiftChangeRequestDates.Add(new ShiftChangeRequestDate
            {
                ShiftChangeRequestId = entity.Id,
                Date                 = date,
                FromDayType          = from,
                ToDayType            = d.ToDayType,
            });
        }
    }

    private async Task<ShiftChangeRequest?> LoadOwnedAsync(Guid userId, int id) =>
        await db.ShiftChangeRequests.FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == userId);

    private async Task<Guid> GetUserIdAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw AppException.Unauthorized("Invalid token claims.");
        return userId;
    }
}
