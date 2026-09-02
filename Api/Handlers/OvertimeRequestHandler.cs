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
/// GET    /overtime-requests           → 列表（支援 ?status=&amp;date=YYYY-MM-DD 篩選，回傳當前使用者資料）
/// POST   /overtime-requests           → 新增（EmployeeId 由 JWT 決定）
/// GET    /overtime-requests/{id}      → 單筆
/// PUT    /overtime-requests/{id}      → 更新（僅 draft 才允許）
/// DELETE /overtime-requests/{id}      → 刪除（僅 draft 才允許）
/// PATCH  /overtime-requests/{id}/submit → 送出（draft → pending）
/// GET    /overtime-requests/estimate    → 加班費即時試算（僅本人）
/// </summary>
public sealed class OvertimeRequestHandler(
    AppDbContext db,
    IOvertimeRequestReadService reader,
    IJwtService jwtService,
    IApprovalNotificationService notifier,
    IApprovalFlowService approvalFlow,
    ICalendarDayReadService calendarReader,
    IWorkPatternReadService workPattern)
{
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        Guid? filterUserId = user?.IsSuperAdmin == true ? null : userId;
        var statusParam = req.Query["status"].ToString();
        var dateParam   = req.Query["date"].ToString();

        // 若有 status 或 date 查詢參數，走篩選路徑並以當前使用者身分過濾
        bool hasFilter = !string.IsNullOrEmpty(statusParam) || !string.IsNullOrEmpty(dateParam);
        if (hasFilter)
        {
            string?   status = string.IsNullOrEmpty(statusParam) ? null : statusParam;
            DateOnly? date   = DateOnly.TryParse(dateParam, out var d) ? d : (DateOnly?)null;

            var filtered = await reader.GetFilteredAsync(status, date, filterUserId);
            return new OkObjectResult(ApiResponse.Ok(filtered));
        }

        // 預設分頁列表
        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        var result = await reader.GetPagedAsync(page, pageSize, filterUserId);
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    public async Task<IActionResult> GetByIdAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid overtime request ID format."));

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var exists = user?.IsSuperAdmin == true
            ? await db.OvertimeRequests.AnyAsync(x => x.Id == intId)
            : await db.OvertimeRequests.AnyAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (!exists)
            return new NotFoundObjectResult(ApiResponse.Fail("Overtime request not found."));

        var item = await reader.GetByIdAsync(intId);
        return new OkObjectResult(ApiResponse.Ok(item));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        // BUG-04: EmployeeId 由 JWT 中的 sub claim 決定，不信任客戶端傳入的值
        var employeeId = await GetUserIdAsync(req);

        var body = await req.ReadFromJsonAsync<CreateOvertimeRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (body.OvertimeDate == default)
            return new BadRequestObjectResult(ApiResponse.Fail("OvertimeDate is required."));

        if (string.IsNullOrWhiteSpace(body.Reason))
            return new BadRequestObjectResult(ApiResponse.Fail("Reason is required."));

        // 關聯專案明細（必填，至少一列）；父表 EstimatedHours 為其合計快取
        var projectRows = await BuildProjectsAsync(body.Projects);

        // 指定審核者存在性驗證
        if (body.DesignatedReviewers is { Length: > 0 })
        {
            var reviewerIds = body.DesignatedReviewers.Select(r => r.ReviewerId).Distinct().ToList();
            var existCount = await db.Users.AsNoTracking().CountAsync(u => reviewerIds.Contains(u.Id));
            if (existCount != reviewerIds.Count)
                return new BadRequestObjectResult(ApiResponse.Fail("一或多位指定審核者不存在。"));
        }

        var item = new OvertimeRequest
        {
            EmployeeId     = employeeId,   // 強制使用 JWT 身分，忽略 body.EmployeeId
            ApprovalItemId = body.ApprovalItemId,
            OvertimeDate   = body.OvertimeDate,
            EstimatedHours = projectRows.Sum(r => r.EstimatedHours),
            // 補償方式二擇一；未知值正規化為補休（安全側，寧可少發現金也不可雙重給付）
            CompensationType = OvertimeCompensationService.Normalize(body.CompensationType),
            Reason         = body.Reason,
            ApprovalStatus = "draft",
            CreatedAt      = Clock.Now,
            Projects       = projectRows,   // EF 一併插入並自動填 FK
        };
        db.OvertimeRequests.Add(item);
        await db.SaveChangesAsync();

        // 儲存指定審核者
        if (body.DesignatedReviewers is { Length: > 0 })
        {
            db.RequestDesignatedReviewers.AddRange(
                DesignatedReviewerHelper.BuildEntities("overtime", item.Id, body.DesignatedReviewers));
            await db.SaveChangesAsync();
        }

        var dto = await reader.GetByIdAsync(item.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Overtime request created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid overtime request ID format."));

        var body = await req.ReadFromJsonAsync<UpdateOvertimeRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.OvertimeRequests.Include(x => x.Projects).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.OvertimeRequests.Include(x => x.Projects).FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("OvertimeRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned overtime requests can be edited.");

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
                .Where(r => r.RequestType == "overtime" && r.RequestId == intId)
                .ToListAsync();
            db.RequestDesignatedReviewers.RemoveRange(old);
            if (body.DesignatedReviewers.Length > 0)
            {
                db.RequestDesignatedReviewers.AddRange(
                    DesignatedReviewerHelper.BuildEntities("overtime", intId, body.DesignatedReviewers));
            }
        }

        // 關聯專案整批替換（必填，不支援省略；一併重算父表 EstimatedHours 合計快取）
        var projectRows = await BuildProjectsAsync(body.Projects);
        db.OvertimeRequestProjects.RemoveRange(item.Projects);
        projectRows.ForEach(p => p.OvertimeRequestId = item.Id);
        await db.OvertimeRequestProjects.AddRangeAsync(projectRows);
        item.EstimatedHours = projectRows.Sum(r => r.EstimatedHours);

        if (body.OvertimeDate.HasValue)    item.OvertimeDate    = body.OvertimeDate.Value;
        if (body.Reason is not null)       item.Reason          = body.Reason;
        if (body.CompensationType is not null)
            item.CompensationType = OvertimeCompensationService.Normalize(body.CompensationType);

        // 日期 / 時數 / 補償方式任一可能已變動 → 舊的加班費快照必須失效，重新送簽時再算一次
        OvertimeCompensationService.ClearSnapshot(item);

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(item.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Overtime request updated."));
    }

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid overtime request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.OvertimeRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.OvertimeRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("OvertimeRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned overtime requests can be deleted.");

        // 一併清除此申請單的審核流程足跡（多型關聯無 FK，須手動刪除，否則殘留列會擋住使用者刪除）
        // 註：OvertimeRequestProjects 有真 FK + Cascade，由 DB 連帶刪除，無須手動處理。
        db.ApprovalRecords.RemoveRange(
            await db.ApprovalRecords.Where(r => r.ApplicationType == "overtime" && r.ApplicationId == item.Id).ToListAsync());
        db.EscalationOverrides.RemoveRange(
            await db.EscalationOverrides.Where(o => o.ApplicationType == "overtime" && o.ApplicationId == item.Id).ToListAsync());
        db.RequestDesignatedReviewers.RemoveRange(
            await db.RequestDesignatedReviewers.Where(r => r.RequestType == "overtime" && r.RequestId == item.Id).ToListAsync());

        db.OvertimeRequests.Remove(item);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Overtime request '{id}' deleted."));
    }

    /// <summary>送出申請（draft → pending）</summary>
    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid overtime request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.OvertimeRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.OvertimeRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("OvertimeRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned overtime requests can be submitted.");

        // 送簽日期只在首次送簽寫入：退回（returned）重送不改，與有單號的申請類型規則一致。
        item.SubmittedAt ??= Clock.Now;

        // 退回重送時清除舊審核記錄，重置指定審核者狀態，重新走流程
        if (item.ApprovalStatus == "returned")
        {
            var oldRecords = await db.ApprovalRecords
                .Where(r => r.ApplicationType == "overtime" && r.ApplicationId == item.Id)
                .ToListAsync();
            db.ApprovalRecords.RemoveRange(oldRecords);

            var oldOverrides = await db.EscalationOverrides
                .Where(o => o.ApplicationType == "overtime" && o.ApplicationId == item.Id)
                .ToListAsync();
            db.EscalationOverrides.RemoveRange(oldOverrides);

            // 重置指定審核者狀態為 pending
            var rdrsToReset = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == "overtime" && r.RequestId == item.Id)
                .ToListAsync();
            foreach (var rdr in rdrsToReset)
            {
                rdr.Status     = "pending";
                rdr.ReviewedAt = null;
                rdr.Comment    = null;
            }
        }

        // 加班費快照：在所有核准分支之前算一次，讓「一般送審 / Superadmin 自動核准 / 全自審自動核准」
        // 三個出口都帶著金額。送審中的單也必須有金額，否則審核者在簽核台是盲簽。
        await OvertimeCompensationService.ApplyAsync(db, calendarReader, workPattern, item);

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
            return new OkObjectResult(ApiResponse.Ok(saDto, "Overtime request auto-approved."));
        }

        // 自動關聯簽核流程（依申請人部門挑流程：部門專屬優先，否則退回通用預設）
        if (item.ApprovalItemId is null)
            item.ApprovalItemId = await approvalFlow.ResolveApprovalItemIdAsync("overtime", submitter?.DepartmentId);

        // 正規化各 designee 所屬步驟並驗證每個指定審核步驟皆有審核者
        await DesignatedReviewerHelper.ValidateAndNormalizeAsync(db, "overtime", item.Id, item.ApprovalItemId, userId);
        await db.SaveChangesAsync();

        // 查詢指定審核者清單傳給 ResolveStartingStepAsync（含 ApprovalStepOrder 綁定步驟）
        var designatedReviewers = await DesignatedReviewerHelper.ReadForFlowAsync(db, "overtime", item.Id);

        // 解析審核步驟（含升級審核邏輯）
        var (startStep, autoApproved, escalation) =
            await approvalFlow.ResolveStartingStepAsync(item.ApprovalItemId, userId, "overtime", designatedReviewers);

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
                ApplicationType  = "overtime",
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
                await notifier.NotifySpecificReviewerAsync("overtime", item.Id, escalation.ReviewerId, userId, escalation.OnBehalfOfUserId is not null);
            else
            {
                // 指定審核步驟（原生 UseApplicantDesignated 或例外指定審核命中）：讀 designee 快照，
                // 與 ResolveStartingStepAsync 的判定同源，確保不會誤走部門/職稱通知
                bool isDesignatedStep = designatedReviewers.Any(r => r.ApprovalStepOrder == startStep);
                if (isDesignatedStep)
                {
                    var firstReviewer = await db.RequestDesignatedReviewers
                        .AsNoTracking()
                        .Where(r => r.RequestType == "overtime" && r.RequestId == item.Id
                                 && r.ApprovalStepOrder == startStep && r.Status == "pending")
                        .OrderBy(r => r.StepOrder)
                        .FirstOrDefaultAsync();
                    if (firstReviewer is not null)
                        await notifier.NotifySpecificReviewerAsync("overtime", item.Id, firstReviewer.ReviewerId, userId, false);
                }
                else
                    await notifier.NotifyReviewersAsync("overtime", item.Id, item.ApprovalItemId, startStep, userId);
            }
        }

        var dto = await reader.GetByIdAsync(item.Id);
        var msg = autoApproved ? "Overtime request auto-approved." : "Overtime request submitted.";
        return new OkObjectResult(ApiResponse.Ok(dto, msg));
    }

    /// <summary>
    /// GET /overtime-requests/estimate?date=&amp;hours= → 加班費即時試算（表單用）。
    /// </summary>
    /// <remarks>
    /// 對象一律取 JWT 的 sub，端點**刻意不接受 employeeId** —— 回傳含時薪，可反推底薪。
    /// 權限沿用 overtime-requests:read（能填加班單的人本來就持有），不另開權限碼。
    /// 比照 GET /leave-requests/working-days 的輕量端點模式：重用 CalendarDayReadService，
    /// 避免把後台行事曆權限強加給申請人。
    /// </remarks>
    public async Task<IActionResult> EstimateAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);

        if (!DateTime.TryParse(req.Query["date"], out var date))
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的加班日期。"));

        if (!decimal.TryParse(req.Query["hours"], out var hours) || hours <= 0)
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的加班時數。"));

        hours = Math.Min(hours, 24m);   // 上界防呆

        var baseSalary = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.BaseSalary)
            .FirstOrDefaultAsync();

        var estimate = await OvertimePayCalculator.CalculateAsync(
            calendarReader, workPattern, baseSalary ?? 0m, userId, date, hours);

        // 同日已有已核准的假日執行活動 → 假日津貼與加班費可能就同一段工時雙重給付，前端顯示警示
        var conflict = await OvertimeCompensationService.HasHolidayTravelConflictAsync(db, userId, date);

        return new OkObjectResult(ApiResponse.Ok(estimate with { HasHolidayTravelConflict = conflict }));
    }

    // ── Helper ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// 驗證並建立關聯專案明細（Create / Update 共用）。
    /// 規則：至少一列、每列時數 &gt; 0、同單不可重複專案、專案必須存在。
    /// </summary>
    private async Task<List<OvertimeRequestProject>> BuildProjectsAsync(OvertimeProjectRequest[]? rows)
    {
        if (rows is null || rows.Length == 0)
            throw AppException.BadRequest("請至少新增一筆關聯專案。");

        if (rows.Any(r => r.EstimatedHours <= 0))
            throw AppException.BadRequest("每個關聯專案的預估時數必須大於 0。");

        var ids = rows.Select(r => r.ProjectId).ToList();
        if (ids.Distinct().Count() != ids.Count)
            throw AppException.BadRequest("同一張加班申請不可重複選擇相同專案。");

        var existCount = await db.Projects.AsNoTracking().CountAsync(p => ids.Contains(p.Id));
        if (existCount != ids.Count)
            throw AppException.BadRequest("一或多個關聯專案不存在。");

        return rows.Select((r, idx) => new OvertimeRequestProject
        {
            ProjectId      = r.ProjectId,
            EstimatedHours = r.EstimatedHours,
            SortOrder      = idx,
        }).ToList();
    }

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
