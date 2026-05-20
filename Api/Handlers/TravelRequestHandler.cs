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
using System.Text.Json;

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
    ICalendarDayReadService calendarDayReader,
    IBlobStorageService blob)
{
    private const string ContainerName = "invoices";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

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

        // 假日執行活動前端送 FormData（含發票檔案上傳），一般出差送 JSON
        if (isHolidayTravel)
            return await CreateFromFormDataAsync(req, employeeId, appType);

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

        var today = Clock.Now;
        var requestNo = await GenerateRequestNoAsync("TR-", today);

        var travelRequest = new TravelRequest
        {
            RequestNo       = requestNo,
            EmployeeId      = employeeId,   // 強制使用 JWT 身分，忽略 body.EmployeeId
            ApprovalItemId  = body.ApprovalItemId,
            Destination     = body.Destination,
            StartDate       = body.StartDate,
            EndDate         = body.EndDate,
            GrandTotal      = items.Sum(i => i.TotalPrice),
            Purpose         = body.Purpose,
            ProjectId       = body.ProjectId,
            IsHolidayTravel = false,
            ApprovalStatus  = "draft",
            CreatedAt       = today,
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

        var dto = await reader.GetByIdAsync(travelRequest.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Travel request created.")) { StatusCode = 201 };
    }

    /// <summary>假日執行活動：從 FormData 建立（不含發票/明細，只保留基本欄位、參與者、指定審核者）</summary>
    private async Task<IActionResult> CreateFromFormDataAsync(HttpRequest req, Guid employeeId, string appType)
    {
        var form = await req.ReadFormAsync();

        var destination = form["destination"].ToString();
        var purpose     = form["purpose"].ToString();
        var projectId   = int.TryParse(form["projectId"], out var pid) ? (int?)pid : null;
        var startDate   = DateTime.TryParse(form["startDate"], out var sd) ? sd : default;
        var endDate     = DateTime.TryParse(form["endDate"], out var ed) ? ed : default;

        if (string.IsNullOrWhiteSpace(destination))
            return new BadRequestObjectResult(ApiResponse.Fail("Destination is required."));
        if (startDate == default || endDate == default)
            return new BadRequestObjectResult(ApiResponse.Fail("StartDate and EndDate are required."));
        if (endDate < startDate)
            return new BadRequestObjectResult(ApiResponse.Fail("EndDate must be on or after StartDate."));

        // 解析指定審核者 JSON
        DesignatedReviewerRequest[]? designatedReviewers = null;
        var drJson = form["designatedReviewers"].ToString();
        if (!string.IsNullOrEmpty(drJson))
            designatedReviewers = JsonSerializer.Deserialize<DesignatedReviewerRequest[]>(drJson, JsonOpts);

        if (designatedReviewers is { Length: > 0 })
        {
            var reviewerIds = designatedReviewers.Select(r => r.ReviewerId).Distinct().ToList();
            var existCount = await db.Users.AsNoTracking().CountAsync(u => reviewerIds.Contains(u.Id));
            if (existCount != reviewerIds.Count)
                return new BadRequestObjectResult(ApiResponse.Fail("一或多位指定審核者不存在。"));
        }

        // 解析參與者 JSON
        ParticipantRequest[]? participants = null;
        var pJson = form["participants"].ToString();
        if (!string.IsNullOrEmpty(pJson))
            participants = JsonSerializer.Deserialize<ParticipantRequest[]>(pJson, JsonOpts);

        if (participants is { Length: > 0 })
        {
            var participantIds = participants.Select(p => p.UserId).Distinct().ToList();
            var existCount = await db.Users.AsNoTracking().CountAsync(u => participantIds.Contains(u.Id));
            if (existCount != participantIds.Count)
                return new BadRequestObjectResult(ApiResponse.Fail("一或多位出差參與者不存在。"));
        }

        var today = Clock.Now;
        var requestNo = await GenerateRequestNoAsync("HTR-", today);

        var travelRequest = new TravelRequest
        {
            RequestNo       = requestNo,
            EmployeeId      = employeeId,
            Destination     = destination,
            StartDate       = startDate,
            EndDate         = endDate,
            GrandTotal      = 0m,
            Purpose         = purpose,
            ProjectId       = projectId,
            IsHolidayTravel = true,
            ApprovalStatus  = "draft",
            CreatedAt       = today,
            Items           = new List<TravelRequestItem>(),
        };
        db.TravelRequests.Add(travelRequest);
        await db.SaveChangesAsync();

        // 儲存指定審核者
        if (designatedReviewers is { Length: > 0 })
        {
            db.RequestDesignatedReviewers.AddRange(
                designatedReviewers.OrderBy(r => r.StepOrder).Select(r => new RequestDesignatedReviewer
                {
                    RequestType = appType,
                    RequestId   = travelRequest.Id,
                    ReviewerId  = r.ReviewerId,
                    StepOrder   = r.StepOrder,
                }));
            await db.SaveChangesAsync();
        }

        // 儲存參與者
        if (participants is { Length: > 0 })
        {
            db.TravelRequestParticipants.AddRange(
                participants.Select(p => new TravelRequestParticipant
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

        // 假日執行活動前端送 FormData（含發票檔案上傳），一般出差送 JSON
        if (isHolidayTravel)
            return await UpdateFromFormDataAsync(req, userId, intId, appType);

        var body = await req.ReadFromJsonAsync<UpdateTravelRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.TravelRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.TravelRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
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

        if (body.Destination is not null)  item.Destination = body.Destination;
        if (body.StartDate.HasValue)       item.StartDate   = body.StartDate.Value;
        if (body.EndDate.HasValue)         item.EndDate     = body.EndDate.Value;
        if (body.Purpose is not null)      item.Purpose     = body.Purpose;
        if (body.ProjectId.HasValue)       item.ProjectId   = body.ProjectId == 0 ? null : body.ProjectId;

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

    /// <summary>假日執行活動：從 FormData 更新（不含發票/明細，只保留基本欄位、參與者、指定審核者）</summary>
    private async Task<IActionResult> UpdateFromFormDataAsync(HttpRequest req, Guid userId, int intId, string appType)
    {
        var form = await req.ReadFormAsync();

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.TravelRequests.Include(x => x.Items).Include(x => x.Participants).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.TravelRequests.Include(x => x.Items).Include(x => x.Participants).FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("TravelRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned travel requests can be edited.");

        // 基本欄位更新
        var destination = form["destination"].ToString();
        if (!string.IsNullOrEmpty(destination)) item.Destination = destination;

        var datesChanged = false;
        if (DateTime.TryParse(form["startDate"], out var sd) && item.StartDate != sd)
        {
            item.StartDate = sd;
            datesChanged = true;
        }
        if (DateTime.TryParse(form["endDate"], out var ed) && item.EndDate != ed)
        {
            item.EndDate = ed;
            datesChanged = true;
        }

        // 假日活動：日期變更時同步重算 HolidayDays（行事曆缺資料時退回 0，由 Submit 階段強制驗證）
        if (item.IsHolidayTravel && datesChanged)
        {
            var hasCalendarData = await calendarDayReader.HasDataForRangeAsync(item.StartDate, item.EndDate);
            item.HolidayDays = hasCalendarData
                ? await calendarDayReader.CountHolidaysAsync(item.StartDate, item.EndDate)
                : 0;
        }

        var purpose = form["purpose"].ToString();
        if (!string.IsNullOrEmpty(purpose)) item.Purpose = purpose;
        if (int.TryParse(form["projectId"], out var pid))
            item.ProjectId = pid == 0 ? null : pid;

        // 指定審核者整組替換
        var drJson = form["designatedReviewers"].ToString();
        if (!string.IsNullOrEmpty(drJson))
        {
            var designatedReviewers = JsonSerializer.Deserialize<DesignatedReviewerRequest[]>(drJson, JsonOpts);
            if (designatedReviewers is { Length: > 0 })
            {
                var reviewerIds = designatedReviewers.Select(r => r.ReviewerId).Distinct().ToList();
                var existCount = await db.Users.AsNoTracking().CountAsync(u => reviewerIds.Contains(u.Id));
                if (existCount != reviewerIds.Count)
                    return new BadRequestObjectResult(ApiResponse.Fail("一或多位指定審核者不存在。"));
            }
            var old = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == appType && r.RequestId == intId)
                .ToListAsync();
            db.RequestDesignatedReviewers.RemoveRange(old);
            if (designatedReviewers is { Length: > 0 })
            {
                db.RequestDesignatedReviewers.AddRange(
                    designatedReviewers.Select(r => new RequestDesignatedReviewer
                    {
                        RequestType = appType,
                        RequestId   = intId,
                        ReviewerId  = r.ReviewerId,
                        StepOrder   = r.StepOrder,
                    }));
            }
        }

        // 參與者整組替換
        var pJson = form["participants"].ToString();
        if (!string.IsNullOrEmpty(pJson))
        {
            var participants = JsonSerializer.Deserialize<ParticipantRequest[]>(pJson, JsonOpts);
            if (participants is { Length: > 0 })
            {
                var participantIds = participants.Select(p => p.UserId).Distinct().ToList();
                var existCount = await db.Users.AsNoTracking().CountAsync(u => participantIds.Contains(u.Id));
                if (existCount != participantIds.Count)
                    return new BadRequestObjectResult(ApiResponse.Fail("一或多位出差參與者不存在。"));
            }
            db.TravelRequestParticipants.RemoveRange(item.Participants);
            if (participants is { Length: > 0 })
            {
                db.TravelRequestParticipants.AddRange(
                    participants.Select(p => new TravelRequestParticipant
                    {
                        TravelRequestId = intId,
                        UserId          = p.UserId,
                        SortOrder       = p.SortOrder,
                    }));
            }
        }

        // 假日活動不再使用明細與發票；若既存資料留有舊 Items，一併清除（含 Blob）
        if (item.Items.Count > 0)
        {
            var oldFileUrls = item.Items
                .Where(i => !string.IsNullOrEmpty(i.FileUrl))
                .Select(i => i.FileUrl!)
                .ToList();

            db.TravelRequestItems.RemoveRange(item.Items);
            item.Items = new List<TravelRequestItem>();

            foreach (var url in oldFileUrls)
            {
                var blobName = blob.ExtractBlobName(url, ContainerName);
                if (blobName is not null)
                    await blob.DeleteAsync(ContainerName, blobName);
            }
        }
        item.GrandTotal = 0m;

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
        // 根據路由決定申請類型（假日執行活動 vs 一般出差）
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

        // 送出前確認有明細項目（假日執行活動不需明細）
        if (!isHolidayTravel)
        {
            var hasItems = await db.TravelRequestItems.AnyAsync(i => i.TravelRequestId == intId);
            if (!hasItems)
                return new BadRequestObjectResult(ApiResponse.Fail("出差申請至少需要一筆費用明細項目。"));
        }

        // 假日執行活動：計算 HolidayDays（送出時計算，需確認行事曆資料已匯入）
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

        if (!user.IsSuperAdmin && !DepartmentCodes.FinancialAndAbove.Contains(user.Department?.Code ?? ""))
            throw AppException.Forbidden("僅財務體系部門或 Superadmin 可更新撥款日。");

        var tr = await db.TravelRequests.FindAsync(intId)
            ?? throw AppException.NotFound("TravelRequest");

        if (tr.ApprovalStatus != "approved")
            return new BadRequestObjectResult(ApiResponse.Fail("只有已核准的出差申請可以設定撥款日。"));

        var body = await req.ReadFromJsonAsync<UpdatePaymentDateRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        // 偵測撥款 / 退款狀態轉換（null → 有值）
        var wasPaidNull     = !tr.PaidAt.HasValue;
        var wasRefundedNull = !tr.RefundedAt.HasValue;

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
        if (body.RefundedAmount.HasValue)
            tr.RefundedAmount = body.RefundedAmount.Value;

        await db.SaveChangesAsync();

        // 首次撥款 / 退款（null → 有值）→ 通知申請人
        if (tr.EmployeeId.HasValue)
        {
            if (wasPaidNull && tr.PaidAt.HasValue)
                await notifier.NotifyApplicantPaidAsync(
                    "travel", tr.Id, tr.EmployeeId.Value, tr.GrandTotal, tr.PaidAt.Value);
            if (wasRefundedNull && tr.RefundedAt.HasValue && tr.RefundedAmount.HasValue)
                await notifier.NotifyApplicantRefundedAsync(
                    "travel", tr.Id, tr.EmployeeId.Value, tr.RefundedAmount.Value, tr.RefundedAt.Value);
        }

        var msg = (body.EstimatedRefundDate.HasValue || body.RefundedAt.HasValue || body.RefundedAmount.HasValue) ? "退款資訊已更新。" : "撥款日期已更新。";
        return new OkObjectResult(ApiResponse.Ok(new { tr.Id, tr.EstimatedPaymentDate, tr.PaidAt, tr.EstimatedRefundDate, tr.RefundedAt, tr.RefundedAmount }, msg));
    }

    /// <summary>
    /// 新增 / 更新出差撥款分期明細（同步維護父表 cache）。
    /// 僅財務體系部門或 Superadmin 可操作。
    /// 每筆新填入 PaidAt 的 installment 觸發一次「已撥款」通知。
    /// </summary>
    public async Task<IActionResult> UpsertInstallmentsAsync(HttpRequest req, string id)
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
            throw AppException.Forbidden("僅財務體系部門或 Superadmin 可設定撥款明細。");

        var tr = await db.TravelRequests
                         .Include(t => t.Installments)
                         .FirstOrDefaultAsync(t => t.Id == intId)
                 ?? throw AppException.NotFound("TravelRequest");

        if (tr.ApprovalStatus != "approved")
            return new BadRequestObjectResult(ApiResponse.Fail("只有已核准的出差申請可以設定撥款明細。"));

        var body = await req.ReadFromJsonAsync<UpsertInstallmentsRequest>(JsonOpts);
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var existingSnap = tr.Installments
            .Select(i => (i.Id, i.InstallmentNo, i.ExpectedDate, i.PaidAt, i.Amount))
            .ToList();
        InstallmentValidator.Validate(body.Installments, tr.GrandTotal, existingSnap);

        var nowUtc = DateTime.UtcNow;
        var taipeiTz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
        var nowTaipei = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, taipeiTz);
        var newlyPaid = new List<NewlyPaidInstallment>();
        var inputIds = body.Installments.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();

        var toRemove = tr.Installments.Where(e => !inputIds.Contains(e.Id)).ToList();
        foreach (var r in toRemove)
            db.TravelRequestInstallments.Remove(r);

        foreach (var input in body.Installments)
        {
            if (input.Id.HasValue)
            {
                var existing = tr.Installments.FirstOrDefault(e => e.Id == input.Id.Value)
                    ?? throw AppException.BadRequest($"找不到要更新的撥款列 Id={input.Id.Value}。");

                var wasPaidNull = !existing.PaidAt.HasValue;
                existing.InstallmentNo = input.InstallmentNo;
                existing.ExpectedDate  = input.ExpectedDate.Date;
                existing.Amount        = input.Amount;
                existing.Note          = input.Note;
                if (input.PaidAt.HasValue)
                {
                    existing.PaidAt = input.PaidAt.Value.Date + nowTaipei.TimeOfDay;
                    if (wasPaidNull)
                    {
                        existing.PaidByUserId = userId;
                        newlyPaid.Add(new(existing.InstallmentNo, existing.PaidAt.Value, existing.Amount, body.Installments.Count));
                    }
                }
                existing.UpdatedAt = nowUtc;
            }
            else
            {
                var ins = new TravelRequestInstallment
                {
                    TravelRequestId = tr.Id,
                    InstallmentNo   = input.InstallmentNo,
                    ExpectedDate    = input.ExpectedDate.Date,
                    Amount          = input.Amount,
                    Note            = input.Note,
                    CreatedAt       = nowUtc,
                    UpdatedAt       = nowUtc,
                };
                if (input.PaidAt.HasValue)
                {
                    ins.PaidAt = input.PaidAt.Value.Date + nowTaipei.TimeOfDay;
                    ins.PaidByUserId = userId;
                    newlyPaid.Add(new(ins.InstallmentNo, ins.PaidAt.Value, ins.Amount, body.Installments.Count));
                }
                db.TravelRequestInstallments.Add(ins);
            }
        }

        var cacheInput = body.Installments
            .Select(i => (i.ExpectedDate, PaidAt: i.PaidAt.HasValue ? i.PaidAt.Value.Date + nowTaipei.TimeOfDay : (DateTime?)null))
            .ToList();
        var (cacheEstimated, cachePaidAt, _) = InstallmentValidator.ComputeCache(cacheInput);
        tr.EstimatedPaymentDate = cacheEstimated;
        tr.PaidAt = cachePaidAt;
        tr.PaidByUserId = cachePaidAt.HasValue ? userId : null;

        await db.SaveChangesAsync();

        if (tr.EmployeeId.HasValue)
            foreach (var np in newlyPaid)
                await notifier.NotifyApplicantPaidAsync(
                    "travel", tr.Id, tr.EmployeeId.Value, np.Amount, np.PaidAt,
                    installmentNo: np.InstallmentNo, totalInstallments: np.TotalInstallments);

        return new OkObjectResult(ApiResponse.Ok(
            new { tr.Id, tr.EstimatedPaymentDate, tr.PaidAt, InstallmentCount = body.Installments.Count },
            $"已更新 {body.Installments.Count} 筆撥款明細。"));
    }

    // ── 假日天數查詢 ────────────────────────────────────────────────────────────

    /// <summary>查詢日期範圍內的假日天數（依行事曆資料）</summary>
    public async Task<IActionResult> CountHolidaysAsync(HttpRequest req)
    {
        if (!DateTime.TryParse(req.Query["startDate"], out var startDate) ||
            !DateTime.TryParse(req.Query["endDate"], out var endDate))
            return new BadRequestObjectResult(ApiResponse.Fail("請提供 startDate 和 endDate 參數。"));

        if (endDate < startDate)
            return new BadRequestObjectResult(ApiResponse.Fail("endDate 不可早於 startDate。"));

        var hasData = await calendarDayReader.HasDataForRangeAsync(startDate, endDate);
        if (!hasData)
            return new OkObjectResult(ApiResponse.Ok(new { holidayDays = (int?)null, hasCalendarData = false },
                "行事曆資料尚未匯入。"));

        var count = await calendarDayReader.CountHolidaysAsync(startDate, endDate);
        return new OkObjectResult(ApiResponse.Ok(new { holidayDays = count, hasCalendarData = true }));
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

    /// <summary>產生出差/假日活動單號：{prefix}yyyyMMdd-NNN（per-prefix-per-day 序號池，唯一索引保護並發）</summary>
    private async Task<string> GenerateRequestNoAsync(string prefix, DateTime today)
    {
        var full = $"{prefix}{today:yyyyMMdd}-";
        var maxNo = await db.TravelRequests
            .Where(t => t.RequestNo.StartsWith(full))
            .MaxAsync(t => (string?)t.RequestNo);
        int seq = 1;
        if (maxNo is not null)
        {
            var seqStr = maxNo[full.Length..];
            if (int.TryParse(seqStr, out var parsed))
                seq = parsed + 1;
        }
        return $"{full}{seq:D3}";
    }
}
