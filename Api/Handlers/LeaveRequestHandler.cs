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
/// GET    /leave-requests/annual-quota       → 年假額度查詢
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
        ["annual", "personal", "sick", "compensatory", "marriage", "bereavement",
         "official", "maternity", "miscarriage_3m", "miscarriage_2to3m",
         "miscarriage_under2m", "prenatal_checkup", "paternity"];

    /// <summary>各假別天數上限（不含年假與補休，它們有獨立邏輯）</summary>
    private static readonly Dictionary<string, int> LeaveTypeDaysLimit = new()
    {
        ["marriage"]            = 8,
        ["maternity"]           = 56,
        ["miscarriage_3m"]      = 28,
        ["miscarriage_2to3m"]   = 7,
        ["miscarriage_under2m"] = 5,
        ["prenatal_checkup"]    = 7,
        ["paternity"]           = 7,
    };

    /// <summary>喪假親屬關係對應天數上限</summary>
    private static readonly Dictionary<string, int> BereavementDaysLimit = new()
    {
        ["spouse"]                 = 8,
        ["parent"]                 = 8,
        ["adoptive_parent"]        = 8,
        ["step_parent"]            = 8,
        ["grandparent"]            = 6,
        ["child"]                  = 6,
        ["spouse_parent"]          = 6,
        ["spouse_adoptive_parent"] = 6,
        ["great_grandparent"]      = 3,
        ["sibling"]                = 3,
        ["spouse_grandparent"]     = 3,
    };

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

        // 喪假必須提供親屬關係
        if (body.LeaveType == "bereavement")
        {
            if (string.IsNullOrWhiteSpace(body.BereavementRelationship))
                return new BadRequestObjectResult(ApiResponse.Fail("喪假必須選擇親屬關係。"));
            if (!BereavementDaysLimit.ContainsKey(body.BereavementRelationship))
                return new BadRequestObjectResult(ApiResponse.Fail("無效的親屬關係。"));
        }

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
        if (body.DesignatedReviewers is { Length: > 0 })
        {
            var reviewerIds = body.DesignatedReviewers.Select(r => r.ReviewerId).Distinct().ToList();
            var existCount = await db.Users.AsNoTracking().CountAsync(u => reviewerIds.Contains(u.Id));
            if (existCount != reviewerIds.Count)
                return new BadRequestObjectResult(ApiResponse.Fail("一或多位指定審核者不存在。"));
        }

        var item = new LeaveRequest
        {
            EmployeeId              = employeeId,   // 強制使用 JWT 身分，忽略 body.EmployeeId
            ApprovalItemId          = body.ApprovalItemId,
            LeaveType               = body.LeaveType,
            StartDate               = body.StartDate,
            EndDate                 = body.EndDate,
            Hours                   = hours,
            Reason                  = body.Reason,
            BereavementRelationship = body.LeaveType == "bereavement" ? body.BereavementRelationship : null,
            ApprovalStatus          = "draft",
            CreatedAt               = Clock.Now,
        };
        db.LeaveRequests.Add(item);
        await db.SaveChangesAsync();

        // 儲存指定審核者
        if (body.DesignatedReviewers is { Length: > 0 })
        {
            db.RequestDesignatedReviewers.AddRange(
                body.DesignatedReviewers.OrderBy(r => r.StepOrder).Select(r => new RequestDesignatedReviewer
                {
                    RequestType = "leave",
                    RequestId   = item.Id,
                    ReviewerId  = r.ReviewerId,
                    StepOrder   = r.StepOrder,
                }));
            await db.SaveChangesAsync();
        }

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

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("LeaveRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned leave requests can be edited.");

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
                .Where(r => r.RequestType == "leave" && r.RequestId == intId)
                .ToListAsync();
            db.RequestDesignatedReviewers.RemoveRange(old);
            if (body.DesignatedReviewers.Length > 0)
            {
                db.RequestDesignatedReviewers.AddRange(
                    body.DesignatedReviewers.Select(r => new RequestDesignatedReviewer
                    {
                        RequestType = "leave",
                        RequestId   = intId,
                        ReviewerId  = r.ReviewerId,
                        StepOrder   = r.StepOrder,
                    }));
            }
        }

        if (body.LeaveType is not null) item.LeaveType = body.LeaveType;
        if (body.StartDate.HasValue)    item.StartDate = body.StartDate.Value;
        if (body.EndDate.HasValue)      item.EndDate   = body.EndDate.Value;
        if (body.Reason is not null)    item.Reason    = body.Reason;

        // 喪假親屬關係更新
        var effectiveLeaveType = body.LeaveType ?? item.LeaveType;
        if (effectiveLeaveType == "bereavement")
        {
            if (body.BereavementRelationship is not null)
                item.BereavementRelationship = body.BereavementRelationship;
        }
        else
        {
            item.BereavementRelationship = null;
        }

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

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("LeaveRequest");

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

    /// <summary>查詢當前使用者的年假額度（根據 HireDate 計算年資）</summary>
    public async Task<IActionResult> GetAnnualQuotaAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.HireDate is null)
            return new OkObjectResult(ApiResponse.Ok(new
            {
                totalDays = 0,
                usedDays = 0m,
                availableDays = 0m,
                seniorityYears = 0,
                seniorityMonths = 0,
                message = "未設定到職日",
            }));

        var now = Clock.Now;
        var (years, months) = CalculateSeniority(user.HireDate.Value, now);
        int totalDays = CalculateAnnualLeaveDays(years, months);

        // 查詢今年已使用的年假天數（pending + approved）
        var startOfYear = new DateTime(now.Year, 1, 1);
        var endOfYear = new DateTime(now.Year, 12, 31, 23, 59, 59);
        var usedHours = await db.LeaveRequests
            .Where(l => l.EmployeeId == userId
                     && l.LeaveType == "annual"
                     && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending")
                     && l.StartDate >= startOfYear && l.StartDate <= endOfYear)
            .SumAsync(l => l.Hours);
        var usedDays = usedHours / 8m;

        return new OkObjectResult(ApiResponse.Ok(new
        {
            totalDays,
            usedDays = Math.Round(usedDays, 1),
            availableDays = Math.Round(Math.Max(0, totalDays - usedDays), 1),
            seniorityYears = years,
            seniorityMonths = months,
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

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("LeaveRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned leave requests can be submitted.");

        // 喪假驗證：必須有親屬關係
        if (item.LeaveType == "bereavement" && string.IsNullOrWhiteSpace(item.BereavementRelationship))
            return new BadRequestObjectResult(ApiResponse.Fail("喪假必須選擇親屬關係。"));

        // 退回重送時清除舊審核記錄，重置指定審核者狀態，重新走流程
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

            // 重置指定審核者狀態為 pending
            var rdrsToReset = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == "leave" && r.RequestId == item.Id)
                .ToListAsync();
            foreach (var rdr in rdrsToReset)
            {
                rdr.Status     = "pending";
                rdr.ReviewedAt = null;
                rdr.Comment    = null;
            }
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

        // 天數上限驗證（累計制）
        var quotaError = await ValidateLeaveQuotaAsync(userId, item);
        if (quotaError is not null)
            return new BadRequestObjectResult(ApiResponse.Fail(quotaError));

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

        // 若流程中有 UseApplicantDesignated 步驟，必須有指定審核者
        if (item.ApprovalItemId.HasValue)
        {
            bool hasDesignatedStep = await db.ApprovalSteps.AsNoTracking()
                .AnyAsync(s => s.ApprovalItemId == item.ApprovalItemId && s.UseApplicantDesignated);
            if (hasDesignatedStep)
            {
                bool hasReviewers = await db.RequestDesignatedReviewers
                    .AnyAsync(r => r.RequestType == "leave" && r.RequestId == item.Id);
                if (!hasReviewers)
                    return new BadRequestObjectResult(ApiResponse.Fail("此簽核流程包含申請人指定審核步驟，請提供指定審核者。"));
            }
        }

        // 查詢指定審核者清單傳給 ResolveStartingStepAsync
        var designatedReviewers = await db.RequestDesignatedReviewers
            .AsNoTracking()
            .Where(r => r.RequestType == "leave" && r.RequestId == item.Id)
            .OrderBy(r => r.StepOrder)
            .Select(r => new DesignatedReviewerRequest(r.ReviewerId, r.StepOrder))
            .ToListAsync();

        // 解析審核步驟（含升級審核邏輯）
        var (startStep, autoApproved, escalation) =
            await approvalFlow.ResolveStartingStepAsync(item.ApprovalItemId, userId, "leave", designatedReviewers);

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
                if (isDesignatedStep)
                {
                    var firstReviewer = await db.RequestDesignatedReviewers
                        .AsNoTracking()
                        .Where(r => r.RequestType == "leave" && r.RequestId == item.Id && r.Status == "pending")
                        .OrderBy(r => r.StepOrder)
                        .FirstOrDefaultAsync();
                    if (firstReviewer is not null)
                        await notifier.NotifySpecificReviewerAsync("leave", item.Id, firstReviewer.ReviewerId, userId, false);
                }
                else
                    await notifier.NotifyReviewersAsync("leave", item.Id, item.ApprovalItemId, startStep, userId);
            }
        }

        var dto = await reader.GetByIdAsync(item.Id);
        var msg = autoApproved ? "Leave request auto-approved." : "Leave request submitted.";
        return new OkObjectResult(ApiResponse.Ok(dto, msg));
    }

    // ── Quota Validation ─────────────────────────────────────────────────────

    /// <summary>驗證假別天數上限（累計制），回傳錯誤訊息或 null</summary>
    private async Task<string?> ValidateLeaveQuotaAsync(Guid userId, LeaveRequest item)
    {
        var now = Clock.Now;

        // 年假額度驗證
        if (item.LeaveType == "annual")
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.HireDate is null)
                return "未設定到職日，無法申請年假。";

            var (years, months) = CalculateSeniority(user.HireDate.Value, now);
            int totalDays = CalculateAnnualLeaveDays(years, months);
            if (totalDays <= 0)
                return "年資不足，尚無年假額度。";

            var usedHours = await GetUsedHoursAsync(userId, "annual", item.Id, now.Year);
            var totalUsedDays = (usedHours + item.Hours) / 8m;
            if (totalUsedDays > totalDays)
                return $"年假額度不足。上限 {totalDays} 天，已使用 {Math.Round(usedHours / 8m, 1)} 天，本次申請 {Math.Round(item.Hours / 8m, 1)} 天。";

            return null;
        }

        // 有固定天數上限的假別
        if (LeaveTypeDaysLimit.TryGetValue(item.LeaveType, out var limit))
        {
            // 產假類別不限年度，其他按年度計算
            bool isMaternityType = item.LeaveType is "maternity" or "miscarriage_3m" or "miscarriage_2to3m" or "miscarriage_under2m" or "prenatal_checkup" or "paternity";
            int? year = isMaternityType ? null : now.Year;

            var usedHours = await GetUsedHoursAsync(userId, item.LeaveType, item.Id, year);
            var totalUsedDays = (usedHours + item.Hours) / 8m;
            if (totalUsedDays > limit)
            {
                var leaveLabel = GetLeaveTypeLabel(item.LeaveType);
                return $"{leaveLabel}額度不足。上限 {limit} 天，已使用 {Math.Round(usedHours / 8m, 1)} 天，本次申請 {Math.Round(item.Hours / 8m, 1)} 天。";
            }
            return null;
        }

        // 喪假：根據親屬關係的天數上限
        if (item.LeaveType == "bereavement")
        {
            if (string.IsNullOrWhiteSpace(item.BereavementRelationship) ||
                !BereavementDaysLimit.TryGetValue(item.BereavementRelationship, out var bLimit))
                return "喪假必須選擇有效的親屬關係。";

            // 喪假按同親屬關係累計（不限年度）
            var usedHours = await db.LeaveRequests
                .Where(l => l.EmployeeId == userId
                         && l.LeaveType == "bereavement"
                         && l.BereavementRelationship == item.BereavementRelationship
                         && l.Id != item.Id
                         && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending"))
                .SumAsync(l => l.Hours);
            var totalUsedDays = (usedHours + item.Hours) / 8m;
            if (totalUsedDays > bLimit)
                return $"喪假額度不足。上限 {bLimit} 天，已使用 {Math.Round(usedHours / 8m, 1)} 天，本次申請 {Math.Round(item.Hours / 8m, 1)} 天。";
            return null;
        }

        // personal / sick / official / compensatory：無天數上限或由其他邏輯驗證
        return null;
    }

    /// <summary>查詢已使用時數（排除當前申請，可選按年度過濾）</summary>
    private async Task<decimal> GetUsedHoursAsync(Guid userId, string leaveType, int excludeId, int? year)
    {
        var query = db.LeaveRequests
            .Where(l => l.EmployeeId == userId
                     && l.LeaveType == leaveType
                     && l.Id != excludeId
                     && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending"));

        if (year.HasValue)
        {
            var startOfYear = new DateTime(year.Value, 1, 1);
            var endOfYear = new DateTime(year.Value, 12, 31, 23, 59, 59);
            query = query.Where(l => l.StartDate >= startOfYear && l.StartDate <= endOfYear);
        }

        return await query.SumAsync(l => l.Hours);
    }

    // ── Seniority / Annual Leave ─────────────────────────────────────────────

    /// <summary>計算年資（年, 月）</summary>
    private static (int Years, int Months) CalculateSeniority(DateTime hireDate, DateTime now)
    {
        int years = now.Year - hireDate.Year;
        int months = now.Month - hireDate.Month;
        if (now.Day < hireDate.Day) months--;
        if (months < 0) { years--; months += 12; }
        return (years, months);
    }

    /// <summary>根據年資計算年假天數</summary>
    private static int CalculateAnnualLeaveDays(int years, int months)
    {
        int totalMonths = years * 12 + months;
        if (totalMonths < 6) return 0;          // 未滿 6 個月
        if (totalMonths < 12) return 3;         // 滿 6 個月 ~ 未滿 1 年
        if (years < 2) return 10;               // 滿 1 年 ~ 未滿 2 年
        if (years < 3) return 10;               // 滿 2 年 ~ 未滿 3 年
        if (years < 5) return 14;               // 滿 3 年 ~ 未滿 5 年
        if (years < 10) return 15;              // 滿 5 年 ~ 未滿 10 年
        return Math.Min(30, 15 + (years - 10)); // 10 年以上：每年加 1 天，上限 30 天
    }

    /// <summary>假別中文標籤（用於錯誤訊息）</summary>
    private static string GetLeaveTypeLabel(string leaveType) => leaveType switch
    {
        "annual"             => "年假",
        "personal"           => "事假",
        "sick"               => "病假",
        "compensatory"       => "補休",
        "marriage"           => "婚假",
        "bereavement"        => "喪假",
        "official"           => "公假",
        "maternity"          => "產假",
        "miscarriage_3m"     => "流產假(3個月以上)",
        "miscarriage_2to3m"  => "流產假(2-3個月)",
        "miscarriage_under2m"=> "流產假(未滿2個月)",
        "prenatal_checkup"   => "產檢假",
        "paternity"          => "陪產假",
        _                    => leaveType,
    };

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
