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
/// 銷假申請（已核准請假單的取消）。
///
/// GET    /leave-requests/{id}/revocable-dates → 可銷假日期清單（逐日勾選用）
/// POST   /leave-requests/{id}/revocations     → 新增銷假草稿
/// GET    /leave-revocations                   → 列表
/// GET    /leave-revocations/{id}              → 單筆
/// PUT    /leave-revocations/{id}              → 更新（僅 draft / returned）
/// DELETE /leave-revocations/{id}              → 刪除（僅 draft / returned）
/// PATCH  /leave-revocations/{id}/submit       → 送出（draft/returned → pending，跑原本的請假簽核流程）
///
/// 簽核掛接：ApprovalItem 以 "leave" 解析（複用請假流程設定），但簽核紀錄 / 指定審核者 /
/// 簽核任務一律以 "leave_revocation" 為 applicationType，避免與同 Id 的請假單撞號。
/// </summary>
public sealed class LeaveRevocationHandler(
    AppDbContext db,
    ILeaveRevocationReadService reader,
    IJwtService jwtService,
    IApprovalNotificationService notifier,
    IApprovalFlowService approvalFlow,
    ICalendarDayReadService calendarReader)
{
    private const string AppType = LeaveRevocationService.AppType;

    // ── Read ─────────────────────────────────────────────────────────────────

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
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid leave revocation ID format."));

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var exists = user?.IsSuperAdmin == true
            ? await db.LeaveRevocations.AnyAsync(x => x.Id == intId)
            : await db.LeaveRevocations.AnyAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (!exists)
            return new NotFoundObjectResult(ApiResponse.Fail("Leave revocation not found."));

        var item = await reader.GetByIdAsync(intId);
        return new OkObjectResult(ApiResponse.Ok(item));
    }

    /// <summary>
    /// 可銷假日期清單。已排除：已被核准銷假的日、被其他進行中銷假單佔用的日、今天以前的日。
    /// GET /leave-requests/{id}/revocable-dates
    /// </summary>
    public async Task<IActionResult> GetRevocableDatesAsync(HttpRequest req, string leaveId)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(leaveId, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid leave request ID format."));

        var (leave, error) = await LoadRevocableLeaveAsync(userId, intId);
        if (error is not null) return error;

        // 編輯既有草稿時排除自己，否則自己已勾的日子會從可選清單消失
        int? excludeId = int.TryParse(req.Query["excludeRevocationId"], out var ex) ? ex : null;
        var days = await GetAvailableDaysAsync(leave!, excludeId);
        var dto = new RevocableDatesDto(
            leave!.Id,
            leave.LeaveType,
            LeaveDayExpander.TimeUnitToString(LeaveDayExpander.GetTimeUnit(leave.LeaveType)),
            leave.StartDate,
            leave.EndDate,
            leave.Hours,
            leave.Reason,
            [.. days.Select(d => new LeaveRevocationDateDto(d.Date, d.Hours))],
            days.Sum(d => d.Hours));
        return new OkObjectResult(ApiResponse.Ok(dto));
    }

    // ── Write ────────────────────────────────────────────────────────────────

    /// <summary>POST /leave-requests/{id}/revocations —— 新增銷假草稿</summary>
    public async Task<IActionResult> CreateAsync(HttpRequest req, string leaveId)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(leaveId, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid leave request ID format."));

        var body = await req.ReadFromJsonAsync<CreateLeaveRevocationRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (string.IsNullOrWhiteSpace(body.Reason))
            return new BadRequestObjectResult(ApiResponse.Fail("請填寫銷假原因。"));

        var (leave, error) = await LoadRevocableLeaveAsync(userId, intId);
        if (error is not null) return error;

        var (picked, pickError) = await ResolvePickedDatesAsync(leave!, body.Dates, excludeRevocationId: null);
        if (pickError is not null) return pickError;

        var revocation = new LeaveRevocation
        {
            LeaveRequestId = leave!.Id,
            EmployeeId     = leave.EmployeeId,
            Reason         = body.Reason,
            RevokedHours   = picked.Sum(d => d.Hours),
            ApprovalStatus = "draft",
            CreatedAt      = Clock.Now,
        };
        db.LeaveRevocations.Add(revocation);
        await db.SaveChangesAsync();

        db.LeaveRevocationDates.AddRange(picked.Select(d => new LeaveRevocationDate
        {
            LeaveRevocationId = revocation.Id,
            Date              = d.Date,
            Hours             = d.Hours,
        }));

        if (body.DesignatedReviewers is { Length: > 0 })
        {
            db.RequestDesignatedReviewers.AddRange(
                DesignatedReviewerHelper.BuildEntities(AppType, revocation.Id, body.DesignatedReviewers));
        }
        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(revocation.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Leave revocation created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid leave revocation ID format."));

        var body = await req.ReadFromJsonAsync<UpdateLeaveRevocationRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var revocation = await LoadOwnedAsync(userId, intId);
        if (revocation is null) throw AppException.NotFound("LeaveRevocation");
        if (revocation.ApprovalStatus is not ("draft" or "returned"))
            throw AppException.BadRequest("Only draft or returned leave revocations can be edited.");

        if (!string.IsNullOrWhiteSpace(body.Reason))
            revocation.Reason = body.Reason;

        if (body.Dates is { Length: > 0 })
        {
            var (leave, error) = await LoadRevocableLeaveAsync(userId, revocation.LeaveRequestId);
            if (error is not null) return error;

            var (picked, pickError) = await ResolvePickedDatesAsync(leave!, body.Dates, excludeRevocationId: revocation.Id);
            if (pickError is not null) return pickError;

            // 逐日明細整批替換
            var oldDates = await db.LeaveRevocationDates.Where(d => d.LeaveRevocationId == revocation.Id).ToListAsync();
            db.LeaveRevocationDates.RemoveRange(oldDates);
            db.LeaveRevocationDates.AddRange(picked.Select(d => new LeaveRevocationDate
            {
                LeaveRevocationId = revocation.Id,
                Date              = d.Date,
                Hours             = d.Hours,
            }));
            revocation.RevokedHours = picked.Sum(d => d.Hours);
        }

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
                    DesignatedReviewerHelper.BuildEntities(AppType, intId, body.DesignatedReviewers));
            }
        }

        await db.SaveChangesAsync();
        var dto = await reader.GetByIdAsync(intId);
        return new OkObjectResult(ApiResponse.Ok(dto, "Leave revocation updated."));
    }

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid leave revocation ID format."));

        var revocation = await LoadOwnedAsync(userId, intId);
        if (revocation is null) throw AppException.NotFound("LeaveRevocation");
        if (revocation.ApprovalStatus is not ("draft" or "returned"))
            throw AppException.BadRequest("Only draft or returned leave revocations can be deleted.");

        // 多型關聯無 FK，須手動清除審核足跡
        db.ApprovalRecords.RemoveRange(
            await db.ApprovalRecords.Where(r => r.ApplicationType == AppType && r.ApplicationId == intId).ToListAsync());
        db.EscalationOverrides.RemoveRange(
            await db.EscalationOverrides.Where(o => o.ApplicationType == AppType && o.ApplicationId == intId).ToListAsync());
        db.RequestDesignatedReviewers.RemoveRange(
            await db.RequestDesignatedReviewers.Where(r => r.RequestType == AppType && r.RequestId == intId).ToListAsync());

        db.LeaveRevocations.Remove(revocation);   // Dates 由 FK cascade 一併刪除
        await db.SaveChangesAsync();
        return new OkObjectResult(ApiResponse.Ok<object?>(null, "Leave revocation deleted."));
    }

    /// <summary>送出銷假申請（draft / returned → pending），重跑一次原本的請假簽核流程。</summary>
    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid leave revocation ID format."));

        var revocation = await LoadOwnedAsync(userId, intId);
        if (revocation is null) throw AppException.NotFound("LeaveRevocation");
        if (revocation.ApprovalStatus is not ("draft" or "returned"))
            throw AppException.BadRequest("Only draft or returned leave revocations can be submitted.");

        // 父單守門 + 逐日重驗（防併發：草稿期間父單可能已被別張銷假單改動）
        var (leave, error) = await LoadRevocableLeaveAsync(userId, revocation.LeaveRequestId);
        if (error is not null) return error;

        var pickedDates = await db.LeaveRevocationDates
            .Where(d => d.LeaveRevocationId == revocation.Id)
            .Select(d => d.Date)
            .ToListAsync();
        if (pickedDates.Count == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("請至少選擇一天要取消的請假日。"));

        var (picked, pickError) = await ResolvePickedDatesAsync(leave!, [.. pickedDates], excludeRevocationId: revocation.Id);
        if (pickError is not null) return pickError;
        revocation.RevokedHours = picked.Sum(d => d.Hours);

        // 退回重送時清除舊審核記錄，重置指定審核者狀態，重新走流程
        if (revocation.ApprovalStatus == "returned")
        {
            db.ApprovalRecords.RemoveRange(
                await db.ApprovalRecords.Where(r => r.ApplicationType == AppType && r.ApplicationId == revocation.Id).ToListAsync());
            db.EscalationOverrides.RemoveRange(
                await db.EscalationOverrides.Where(o => o.ApplicationType == AppType && o.ApplicationId == revocation.Id).ToListAsync());

            var rdrsToReset = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == AppType && r.RequestId == revocation.Id)
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
            revocation.ApprovalStatus   = "approved";
            revocation.CurrentStepOrder = 1;
            revocation.ReviewedAt       = Clock.Now;
            revocation.ReviewedById     = userId;
            revocation.ReviewNote       = "系統自動核准（Superadmin）";
            await LeaveRevocationService.ApplyAsync(db, calendarReader, revocation);
            await db.SaveChangesAsync();
            await notifier.NotifyLeaveRevocationAgentAsync(revocation.Id);
            var saDto = await reader.GetByIdAsync(revocation.Id);
            return new OkObjectResult(ApiResponse.Ok(saDto, "Leave revocation auto-approved."));
        }

        // 沿用請假流程設定（依申請人部門挑流程），但簽核紀錄一律掛 leave_revocation
        revocation.ApprovalItemId ??= await approvalFlow.ResolveApprovalItemIdAsync("leave", submitter?.DepartmentId);

        await DesignatedReviewerHelper.ValidateAndNormalizeAsync(db, AppType, revocation.Id, revocation.ApprovalItemId, userId);
        await db.SaveChangesAsync();

        var designatedReviewers = await DesignatedReviewerHelper.ReadForFlowAsync(db, AppType, revocation.Id);

        // requestDays 帶「原假單天數」而非本次銷假天數：銷假要回到與原假單相同的那組審核關卡
        var (startStep, autoApproved, escalation) =
            await approvalFlow.ResolveStartingStepAsync(revocation.ApprovalItemId, userId, AppType, designatedReviewers,
                requestDays: (leave!.OriginalHours ?? leave.Hours) / 8m);

        if (autoApproved)
        {
            revocation.ApprovalStatus   = "approved";
            revocation.CurrentStepOrder = startStep;
            revocation.ReviewedAt       = Clock.Now;
            revocation.ReviewedById     = userId;
            revocation.ReviewNote       = "系統自動核准（所有審核步驟皆為申請人本人）";
            await LeaveRevocationService.ApplyAsync(db, calendarReader, revocation);
        }
        else
        {
            revocation.ApprovalStatus   = "pending";
            revocation.CurrentStepOrder = startStep;
        }

        if (escalation is not null)
        {
            db.EscalationOverrides.Add(new EscalationOverride
            {
                ApplicationType  = AppType,
                ApplicationId    = revocation.Id,
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
                await notifier.NotifySpecificReviewerAsync(AppType, revocation.Id, escalation.ReviewerId, userId, escalation.OnBehalfOfUserId is not null);
            else
            {
                bool isDesignatedStep = designatedReviewers.Any(r => r.ApprovalStepOrder == startStep);
                if (isDesignatedStep)
                {
                    var firstReviewer = await db.RequestDesignatedReviewers
                        .AsNoTracking()
                        .Where(r => r.RequestType == AppType && r.RequestId == revocation.Id
                                 && r.ApprovalStepOrder == startStep && r.Status == "pending")
                        .OrderBy(r => r.StepOrder)
                        .FirstOrDefaultAsync();
                    if (firstReviewer is not null)
                        await notifier.NotifySpecificReviewerAsync(AppType, revocation.Id, firstReviewer.ReviewerId, userId, false);
                }
                else
                    await notifier.NotifyReviewersAsync(AppType, revocation.Id, revocation.ApprovalItemId, startStep, userId);
            }
        }
        else
            await notifier.NotifyLeaveRevocationAgentAsync(revocation.Id);

        var dto = await reader.GetByIdAsync(revocation.Id);
        var msg = autoApproved ? "Leave revocation auto-approved." : "Leave revocation submitted.";
        return new OkObjectResult(ApiResponse.Ok(dto, msg));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<LeaveRevocation?> LoadOwnedAsync(Guid userId, int id)
    {
        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        return currentUser?.IsSuperAdmin == true
            ? await db.LeaveRevocations.FirstOrDefaultAsync(x => x.Id == id)
            : await db.LeaveRevocations.FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == userId);
    }

    /// <summary>載入可銷假的父單並套用共同守門：本人（或 Superadmin）、已核准、假期尚未結束。</summary>
    private async Task<(LeaveRequest? leave, IActionResult? error)> LoadRevocableLeaveAsync(Guid userId, int leaveId)
    {
        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var leave = currentUser?.IsSuperAdmin == true
            ? await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == leaveId)
            : await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == leaveId && x.EmployeeId == userId);

        if (leave is null)
            return (null, new NotFoundObjectResult(ApiResponse.Fail("Leave request not found.")));
        if (leave.ApprovalStatus != "approved")
            return (null, new BadRequestObjectResult(ApiResponse.Fail("僅已核准的請假申請可以銷假。")));
        if (leave.EndDate < Clock.Now)
            return (null, new BadRequestObjectResult(ApiResponse.Fail("假期已結束，無法銷假。")));

        return (leave, null);
    }

    /// <summary>
    /// 尚可銷假的日子 ＝ 逐日展開 − 已被核准銷假的日 − 被其他進行中銷假單佔用的日 − 今天以前的日。
    /// 限制「今天（含）以後」是為了避免更動已休完的日子造成出勤紀錄與已結算薪資不一致。
    /// </summary>
    private async Task<List<LeaveDay>> GetAvailableDaysAsync(LeaveRequest leave, int? excludeRevocationId = null)
    {
        var taken = await db.LeaveRevocationDates
            .AsNoTracking()
            .Where(d => d.LeaveRevocation!.LeaveRequestId == leave.Id
                     && d.LeaveRevocation.ApprovalStatus != "rejected"
                     && (excludeRevocationId == null || d.LeaveRevocationId != excludeRevocationId))
            .Select(d => d.Date)
            .ToListAsync();
        var takenSet = taken.Select(d => d.Date).ToHashSet();

        var today = Clock.Now.Date;
        var all = await LeaveDayExpander.ExpandAsync(calendarReader, leave);
        return [.. all.Where(d => d.Date.Date >= today && !takenSet.Contains(d.Date.Date))];
    }

    /// <summary>驗證使用者勾選的日期都仍可銷，並回傳帶正確時數的逐日清單。</summary>
    private async Task<(List<LeaveDay> picked, IActionResult? error)> ResolvePickedDatesAsync(
        LeaveRequest leave, DateTime[] requested, int? excludeRevocationId)
    {
        if (requested.Length == 0)
            return ([], new BadRequestObjectResult(ApiResponse.Fail("請至少選擇一天要取消的請假日。")));

        var available = await GetAvailableDaysAsync(leave, excludeRevocationId);
        var byDate = available.ToDictionary(d => d.Date.Date);

        var picked = new List<LeaveDay>();
        foreach (var date in requested.Select(d => d.Date).Distinct().OrderBy(d => d))
        {
            if (!byDate.TryGetValue(date, out var day))
                return ([], new BadRequestObjectResult(ApiResponse.Fail(
                    $"{date:yyyy/MM/dd} 不是可銷假的日期（可能已銷假、已有進行中的銷假申請，或已經過去）。")));
            picked.Add(day);
        }
        return (picked, null);
    }

    private async Task<Guid> GetUserIdAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw AppException.Unauthorized("Invalid token claims.");
        return userId;
    }
}
