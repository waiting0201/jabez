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

public sealed class TravelWriteOffRequestHandler(
    AppDbContext db,
    ITravelWriteOffRequestReadService reader,
    IBlobStorageService blob,
    IJwtService jwtService,
    IApprovalNotificationService notifier,
    IApprovalFlowService approvalFlow)
{
    private const string ContainerName = "travel-write-off-invoices";
    private const string RequestType   = "travel_write_off";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>出差沖銷明細 multipart JSON 的內部結構</summary>
    private sealed record TravelWriteOffItemMetadata(
        string    Category,
        int       SeqNo,
        string    ItemName,
        decimal   UnitPrice,
        string    Quantity,
        decimal   TotalPrice,
        string?   Note,
        string?   InvoiceNo,
        string?   FileName,
        string?   FileUrl,
        int       FileIndex,
        int       SortOrder,
        DateTime? InvoiceDate = null);

    // ── 可沖銷的出差申請清單（已核准、未結案）────────────────────────────

    public async Task<IActionResult> GetAvailableTravelsAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        bool isSuperAdmin = user?.IsSuperAdmin == true;

        var list = await db.TravelRequests
            .AsNoTracking()
            .Include(t => t.Project)
            .Where(t => t.ApprovalStatus == "approved"
                     && !t.IsClosed
                     && (isSuperAdmin || t.EmployeeId == userId))
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.Destination,
                t.StartDate,
                t.EndDate,
                t.Purpose,
                ProjectCode = t.Project != null ? t.Project.Code : "",
                t.GrandTotal,
                WrittenOffTotal = db.TravelWriteOffRecords
                    .Where(w => w.TravelRequestId == t.Id && w.ApprovalStatus != "rejected")
                    .Sum(w => (decimal?)w.GrandTotal) ?? 0m,
            })
            .ToListAsync();

        return new OkObjectResult(ApiResponse.Ok(list));
    }

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
        var userId = await GetUserIdAsync(req);

        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel write-off request ID format."));

        if (!await db.TravelWriteOffRecords.AnyAsync(x => x.Id == intId))
            return new NotFoundObjectResult(ApiResponse.Fail("Travel write-off request not found."));

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.IsSuperAdmin != true)
        {
            bool isSubmitter = await db.TravelWriteOffRecords.AnyAsync(x => x.Id == intId && x.SubmittedById == userId);
            bool hasReviewed = await db.ApprovalRecords.AsNoTracking()
                .AnyAsync(ar => ar.ApplicationType == "travel_write_off" && ar.ApplicationId == intId && ar.ReviewedById == userId);
            bool isDesignated = await db.RequestDesignatedReviewers.AsNoTracking()
                .AnyAsync(r => r.RequestType == "travel_write_off" && r.RequestId == intId && r.ReviewerId == userId);

            if (!isSubmitter && !hasReviewed && !isDesignated)
                return new NotFoundObjectResult(ApiResponse.Fail("Travel write-off request not found."));
        }

        var item = await reader.GetByIdAsync(intId);
        return new OkObjectResult(ApiResponse.Ok(item));
    }

    // ── 新增（multipart/form-data，支援發票檔案上傳）──────────────────────

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var submittedById = await GetUserIdAsync(req);

        var form = await req.ReadFormAsync();
        var travelRequestIdStr      = form["travelRequestId"].ToString();
        var itemsJson               = form["items"].ToString();
        var note                    = form["note"].ToString();
        var designatedReviewersJson = form["designatedReviewers"].ToString();

        if (!int.TryParse(travelRequestIdStr, out var travelRequestId))
            return new BadRequestObjectResult(ApiResponse.Fail("travelRequestId 欄位為必填且須為整數。"));

        // 驗證出差申請存在、已核准、未結案
        var submitter = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == submittedById);
        var tr = await db.TravelRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == travelRequestId
                && (submitter != null && submitter.IsSuperAdmin || x.EmployeeId == submittedById))
            ?? throw AppException.NotFound("TravelRequest");

        if (tr.ApprovalStatus != "approved")
            throw AppException.BadRequest("Only approved travel requests can have write-offs.");

        if (tr.IsClosed)
            throw AppException.BadRequest("此出差申請已結案，無法再沖銷。");

        // 解析沖銷明細
        if (string.IsNullOrWhiteSpace(itemsJson))
            return new BadRequestObjectResult(ApiResponse.Fail("items 欄位為必填。"));

        var items = JsonSerializer.Deserialize<TravelWriteOffItemMetadata[]>(itemsJson, JsonOpts);
        if (items is null || items.Length == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("At least one write-off item is required."));

        // 解析指定審核者
        DesignatedReviewerRequest[]? designatedReviewers = null;
        if (!string.IsNullOrEmpty(designatedReviewersJson))
            designatedReviewers = JsonSerializer.Deserialize<DesignatedReviewerRequest[]>(designatedReviewersJson, JsonOpts);

        // 指定審核者存在性驗證
        if (designatedReviewers is { Length: > 0 })
        {
            var reviewerIds = designatedReviewers.Select(r => r.ReviewerId).Distinct().ToList();
            var existCount = await db.Users.AsNoTracking().CountAsync(u => reviewerIds.Contains(u.Id));
            if (existCount != reviewerIds.Count)
                return new BadRequestObjectResult(ApiResponse.Fail("一或多位指定審核者不存在。"));
        }

        var grandTotal = items.Sum(i => i.TotalPrice);

        // 批次內發票號碼重複檢查
        await ValidateInvoiceUniquenessAsync(items, excludeTravelWriteOffRecordId: null);

        // 取得下一個沖銷編號（WriteOffNo：本出差申請的第幾次沖銷）
        var lastNo = await db.TravelWriteOffRecords
            .Where(w => w.TravelRequestId == travelRequestId)
            .OrderByDescending(w => w.WriteOffNo)
            .Select(w => w.WriteOffNo)
            .FirstOrDefaultAsync();

        // 產生沖銷申請單號：TWO-yyyyMMdd-NNN（唯一索引保護並發）
        var today  = Clock.Now;
        var prefix = $"TWO-{today:yyyyMMdd}-";
        var maxNo  = await db.TravelWriteOffRecords
            .Where(w => w.RequestNo.StartsWith(prefix))
            .MaxAsync(w => (string?)w.RequestNo);
        int seq = 1;
        if (maxNo is not null)
        {
            var seqStr = maxNo[prefix.Length..];
            if (int.TryParse(seqStr, out var parsed))
                seq = parsed + 1;
        }
        var requestNo = $"{prefix}{seq:D3}";

        // 上傳檔案至 Blob Storage，組裝沖銷明細
        var files              = form.Files.GetFiles("files");
        var travelWriteOffItems = await BuildTravelWriteOffItemsAsync(items, files);

        var wo = new TravelWriteOffRecord
        {
            RequestNo       = requestNo,
            TravelRequestId = travelRequestId,
            WriteOffNo      = lastNo + 1,
            GrandTotal      = grandTotal,
            Note            = note,
            SubmittedById   = submittedById,
            ApprovalStatus  = "draft",
            CurrentStepOrder = 1,
            CreatedAt       = today,
        };
        wo.Items = travelWriteOffItems;

        db.TravelWriteOffRecords.Add(wo);
        await db.SaveChangesAsync();

        // 儲存指定審核者
        if (designatedReviewers is { Length: > 0 })
        {
            db.RequestDesignatedReviewers.AddRange(
                designatedReviewers.OrderBy(r => r.StepOrder).Select(r => new RequestDesignatedReviewer
                {
                    RequestType = RequestType,
                    RequestId   = wo.Id,
                    ReviewerId  = r.ReviewerId,
                    StepOrder   = r.StepOrder,
                }));
            await db.SaveChangesAsync();
        }

        var dto = await reader.GetByIdAsync(wo.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Travel write-off request created.")) { StatusCode = 201 };
    }

    // ── 更新草稿（multipart/form-data）──────────────────────────────────────

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);

        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel write-off request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var wo = currentUser?.IsSuperAdmin == true
            ? await db.TravelWriteOffRecords.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.TravelWriteOffRecords.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (wo is null) throw AppException.NotFound("TravelWriteOffRecord");

        if (wo.ApprovalStatus != "draft" && wo.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned travel write-off requests can be edited.");

        var form      = await req.ReadFormAsync();
        var note      = form["note"].ToString();
        var itemsJson = form["items"].ToString();
        var designatedReviewersJson = form["designatedReviewers"].ToString();

        // 備注欄位（提供時才更新）
        if (form.ContainsKey("note"))
            wo.Note = string.IsNullOrWhiteSpace(note) ? null : note;

        // 指定審核者整組替換（提供時才更新）
        if (!string.IsNullOrEmpty(designatedReviewersJson))
        {
            var updateDesignatedReviewers = JsonSerializer.Deserialize<DesignatedReviewerRequest[]>(designatedReviewersJson, JsonOpts);
            if (updateDesignatedReviewers is not null)
            {
                if (updateDesignatedReviewers.Length > 0)
                {
                    var reviewerIds = updateDesignatedReviewers.Select(r => r.ReviewerId).Distinct().ToList();
                    var existCount = await db.Users.AsNoTracking().CountAsync(u => reviewerIds.Contains(u.Id));
                    if (existCount != reviewerIds.Count)
                        return new BadRequestObjectResult(ApiResponse.Fail("一或多位指定審核者不存在。"));
                }

                var old = await db.RequestDesignatedReviewers
                    .Where(r => r.RequestType == RequestType && r.RequestId == intId)
                    .ToListAsync();
                db.RequestDesignatedReviewers.RemoveRange(old);

                if (updateDesignatedReviewers.Length > 0)
                {
                    db.RequestDesignatedReviewers.AddRange(
                        updateDesignatedReviewers.Select(r => new RequestDesignatedReviewer
                        {
                            RequestType = RequestType,
                            RequestId   = intId,
                            ReviewerId  = r.ReviewerId,
                            StepOrder   = r.StepOrder,
                        }));
                }
            }
        }

        // 更新沖銷明細（提供時才更新）
        if (!string.IsNullOrWhiteSpace(itemsJson))
        {
            var items = JsonSerializer.Deserialize<TravelWriteOffItemMetadata[]>(itemsJson, JsonOpts);
            if (items is null || items.Length == 0)
                return new BadRequestObjectResult(ApiResponse.Fail("At least one write-off item is required."));

            // 發票唯一性驗證（排除本筆記錄自身的明細）
            await ValidateInvoiceUniquenessAsync(items, excludeTravelWriteOffRecordId: intId);

            var newGrandTotal = items.Sum(i => i.TotalPrice);

            // 收集舊 FileUrl（比對後刪除不再使用的 blob）
            var oldFileUrls = wo.Items
                .Where(i => !string.IsNullOrEmpty(i.FileUrl))
                .Select(i => i.FileUrl!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 上傳新檔案、保留既有 URL
            var files                = form.Files.GetFiles("files");
            var newWriteOffItems     = new List<TravelWriteOffItem>();
            var newFileUrls          = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (item, idx) in items.Select((v, i) => (v, i)))
            {
                string? fileUrl = item.FileUrl; // 保留既有 URL
                if (item.FileIndex >= 0 && item.FileIndex < files.Count)
                {
                    var file = files[item.FileIndex];
                    var ext  = Path.GetExtension(file.FileName);
                    var blobName = $"{Clock.Now:yyyy/MM}/{Guid.NewGuid()}{ext}";
                    using var stream = file.OpenReadStream();
                    fileUrl = await blob.UploadAsync(ContainerName, blobName, stream, file.ContentType);
                }
                if (!string.IsNullOrEmpty(fileUrl))
                    newFileUrls.Add(fileUrl);

                newWriteOffItems.Add(new TravelWriteOffItem
                {
                    TravelWriteOffRecordId = intId,
                    Category               = item.Category,
                    SeqNo                  = item.SeqNo,
                    ItemName               = item.ItemName,
                    UnitPrice              = item.UnitPrice,
                    Quantity               = item.Quantity,
                    TotalPrice             = item.TotalPrice,
                    Note                   = item.Note,
                    InvoiceNo              = item.InvoiceNo,
                    FileName               = item.FileName,
                    FileUrl                = fileUrl,
                    InvoiceDate            = item.InvoiceDate,
                    SortOrder              = item.SortOrder > 0 ? item.SortOrder : idx,
                });
            }

            db.TravelWriteOffItems.RemoveRange(wo.Items);
            wo.Items      = newWriteOffItems;
            wo.GrandTotal = newGrandTotal;

            await db.SaveChangesAsync();

            // 刪除不再使用的舊 blob
            var removedUrls = oldFileUrls.Except(newFileUrls);
            foreach (var url in removedUrls)
            {
                var blobName = blob.ExtractBlobName(url, ContainerName);
                if (blobName is not null)
                    await blob.DeleteAsync(ContainerName, blobName);
            }
        }
        else
        {
            await db.SaveChangesAsync();
        }

        var dto = await reader.GetByIdAsync(wo.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Travel write-off request updated."));
    }

    // ── 刪除（僅草稿）────────────────────────────────────────────────────────

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);

        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel write-off request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var wo = currentUser?.IsSuperAdmin == true
            ? await db.TravelWriteOffRecords.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.TravelWriteOffRecords.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (wo is null) throw AppException.NotFound("TravelWriteOffRecord");

        if (wo.ApprovalStatus != "draft" && wo.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned travel write-off requests can be deleted.");

        // 收集要刪除的 blob
        var blobNames = wo.Items
            .Select(i => blob.ExtractBlobName(i.FileUrl, ContainerName))
            .Where(n => n is not null)
            .ToList();

        db.TravelWriteOffRecords.Remove(wo);
        await db.SaveChangesAsync();

        // 刪除 blob files（在 DB 刪除後才清理，避免孤兒資料）
        foreach (var name in blobNames)
            await blob.DeleteAsync(ContainerName, name!);

        return new OkObjectResult(ApiResponse.Ok($"Travel write-off request '{id}' deleted."));
    }

    // ── 送出申請（draft/returned → pending）──────────────────────────────────

    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);

        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel write-off request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var wo = currentUser?.IsSuperAdmin == true
            ? await db.TravelWriteOffRecords.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.TravelWriteOffRecords.FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (wo is null) throw AppException.NotFound("TravelWriteOffRecord");

        if (wo.ApprovalStatus != "draft" && wo.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned travel write-off requests can be submitted.");

        // 退回重送：清除舊審核記錄，重置指定審核者狀態
        if (wo.ApprovalStatus == "returned")
        {
            var oldRecords = await db.ApprovalRecords
                .Where(r => r.ApplicationType == RequestType && r.ApplicationId == wo.Id)
                .ToListAsync();
            db.ApprovalRecords.RemoveRange(oldRecords);

            var oldOverrides = await db.EscalationOverrides
                .Where(o => o.ApplicationType == RequestType && o.ApplicationId == wo.Id)
                .ToListAsync();
            db.EscalationOverrides.RemoveRange(oldOverrides);

            // 重置指定審核者狀態為 pending
            var rdrsToReset = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == RequestType && r.RequestId == wo.Id)
                .ToListAsync();
            foreach (var rdr in rdrsToReset)
            {
                rdr.Status     = "pending";
                rdr.ReviewedAt = null;
                rdr.Comment    = null;
            }
        }

        // 自動關聯簽核流程（依 ApplicationType 查找啟用的流程）
        if (wo.ApprovalItemId is null)
        {
            var flow = await db.ApprovalItems
                .AsNoTracking()
                .FirstOrDefaultAsync(ai => ai.ApplicationType == RequestType && ai.IsActive);
            if (flow is not null)
                wo.ApprovalItemId = flow.Id;
        }

        // Superadmin 無部門歸屬，直接自動核准
        var submitter = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (submitter?.IsSuperAdmin == true)
        {
            wo.ApprovalStatus    = "approved";
            wo.CurrentStepOrder  = 1;
            wo.ReviewedAt        = Clock.Now;
            wo.ReviewedById      = userId;
            wo.ReviewNote        = "系統自動核准（Superadmin）";
            await db.SaveChangesAsync();
            var saDto = await reader.GetByIdAsync(wo.Id);
            return new OkObjectResult(ApiResponse.Ok(saDto, "Travel write-off request auto-approved."));
        }

        // 若流程中有 UseApplicantDesignated 步驟，必須有指定審核者
        if (wo.ApprovalItemId.HasValue)
        {
            bool hasDesignatedStep = await db.ApprovalSteps.AsNoTracking()
                .AnyAsync(s => s.ApprovalItemId == wo.ApprovalItemId && s.UseApplicantDesignated);
            if (hasDesignatedStep)
            {
                bool hasReviewers = await db.RequestDesignatedReviewers
                    .AnyAsync(r => r.RequestType == RequestType && r.RequestId == wo.Id);
                if (!hasReviewers)
                    return new BadRequestObjectResult(ApiResponse.Fail("此簽核流程包含申請人指定審核步驟，請提供指定審核者。"));
            }
        }

        // 查詢指定審核者清單傳給 ResolveStartingStepAsync
        var designatedReviewers = await db.RequestDesignatedReviewers
            .AsNoTracking()
            .Where(r => r.RequestType == RequestType && r.RequestId == wo.Id)
            .OrderBy(r => r.StepOrder)
            .Select(r => new DesignatedReviewerRequest(r.ReviewerId, r.StepOrder))
            .ToListAsync();

        // 自審跳過邏輯（與請款、預支、沖銷一致，不升級）
        var (startStep, autoApproved, _) = await approvalFlow.ResolveStartingStepAsync(
            wo.ApprovalItemId, userId, RequestType, designatedReviewers);

        if (autoApproved)
        {
            wo.ApprovalStatus    = "approved";
            wo.CurrentStepOrder  = startStep;
            wo.ReviewedAt        = Clock.Now;
            wo.ReviewedById      = userId;
            wo.ReviewNote        = "系統自動核准（所有審核步驟皆為申請人本人）";
        }
        else
        {
            wo.ApprovalStatus    = "pending";
            wo.CurrentStepOrder  = startStep;
        }

        await db.SaveChangesAsync();

        // 通知審核者：指定審核步驟通知第一位指定審核者，否則通知符合條件的審核者
        if (!autoApproved && wo.SubmittedById.HasValue)
        {
            bool isDesignatedStep = wo.ApprovalItemId.HasValue && await db.ApprovalSteps.AsNoTracking()
                .AnyAsync(s => s.ApprovalItemId == wo.ApprovalItemId
                    && s.StepOrder == startStep
                    && s.UseApplicantDesignated);

            if (isDesignatedStep)
            {
                var firstReviewer = await db.RequestDesignatedReviewers
                    .AsNoTracking()
                    .Where(r => r.RequestType == RequestType && r.RequestId == wo.Id && r.Status == "pending")
                    .OrderBy(r => r.StepOrder)
                    .FirstOrDefaultAsync();
                if (firstReviewer is not null)
                    await notifier.NotifySpecificReviewerAsync(RequestType, wo.Id, firstReviewer.ReviewerId, wo.SubmittedById.Value, false);
            }
            else
                await notifier.NotifyReviewersAsync(RequestType, wo.Id, wo.ApprovalItemId, startStep, wo.SubmittedById.Value);
        }

        var dto = await reader.GetByIdAsync(wo.Id);
        var msg = autoApproved ? "Travel write-off request auto-approved." : "Travel write-off request submitted.";
        return new OkObjectResult(ApiResponse.Ok(dto, msg));
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    /// <summary>從 JWT Bearer Token 取出 sub claim 作為使用者 GUID</summary>
    private async Task<Guid> GetUserIdAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw AppException.Unauthorized("Invalid token claims.");
        return userId;
    }

    /// <summary>
    /// 驗證發票號碼唯一性：批次內去重 + 跨所有出差沖銷、預支沖銷與請款發票表（排除已拒絕申請）。
    /// excludeTravelWriteOffRecordId：更新時傳入自身 ID 以排除自身明細。
    /// </summary>
    private async Task ValidateInvoiceUniquenessAsync(
        TravelWriteOffItemMetadata[] items,
        int? excludeTravelWriteOffRecordId)
    {
        var invoiceNos = items
            .Where(i => !string.IsNullOrWhiteSpace(i.InvoiceNo))
            .Select(i => i.InvoiceNo!)
            .ToList();

        if (invoiceNos.Count == 0) return;

        // 批次內重複檢查
        var duplicatesInBatch = invoiceNos
            .GroupBy(n => n)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicatesInBatch.Count > 0)
            throw AppException.Conflict($"發票號碼重複：{string.Join(", ", duplicatesInBatch)}");

        // 資料庫唯一性檢查（跨出差沖銷 + 預支沖銷 + 請款發票，排除已拒絕的申請）
        var travelWriteOffQuery = db.TravelWriteOffItems
            .Where(twi => invoiceNos.Contains(twi.InvoiceNo!));

        // 更新場景：排除本筆 TravelWriteOffRecord 的明細
        if (excludeTravelWriteOffRecordId.HasValue)
            travelWriteOffQuery = travelWriteOffQuery.Where(twi => twi.TravelWriteOffRecordId != excludeTravelWriteOffRecordId.Value);

        var existInTravelWriteOff = await travelWriteOffQuery
            .Select(twi => twi.InvoiceNo!)
            .Distinct()
            .ToListAsync();

        var existInWriteOff = await db.WriteOffItems
            .Where(wi => invoiceNos.Contains(wi.InvoiceNo!))
            .Select(wi => wi.InvoiceNo!)
            .Distinct()
            .ToListAsync();

        var existInInvoice = await db.InvoiceItems
            .Where(ii => invoiceNos.Contains(ii.InvoiceNo)
                      && ii.PaymentRequest.ApprovalStatus != "rejected")
            .Select(ii => ii.InvoiceNo)
            .Distinct()
            .ToListAsync();

        var existingNos = existInTravelWriteOff.Union(existInWriteOff).Union(existInInvoice).Distinct().ToList();
        if (existingNos.Count > 0)
            throw AppException.Conflict($"發票號碼已存在：{string.Join(", ", existingNos)}");
    }

    /// <summary>
    /// 依 multipart metadata 與上傳檔案清單，組裝 TravelWriteOffItem 清單並上傳至 Blob Storage。
    /// </summary>
    private async Task<List<TravelWriteOffItem>> BuildTravelWriteOffItemsAsync(
        TravelWriteOffItemMetadata[] items,
        IReadOnlyList<IFormFile> files)
    {
        var result = new List<TravelWriteOffItem>(items.Length);

        foreach (var (item, idx) in items.Select((v, i) => (v, i)))
        {
            string? fileUrl = null;

            if (item.FileIndex >= 0 && item.FileIndex < files.Count)
            {
                var file = files[item.FileIndex];
                var ext  = Path.GetExtension(file.FileName);
                var blobName = $"{Clock.Now:yyyy/MM}/{Guid.NewGuid()}{ext}";
                using var stream = file.OpenReadStream();
                fileUrl = await blob.UploadAsync(ContainerName, blobName, stream, file.ContentType);
            }

            result.Add(new TravelWriteOffItem
            {
                Category               = item.Category,
                SeqNo                  = item.SeqNo,
                ItemName               = item.ItemName,
                UnitPrice              = item.UnitPrice,
                Quantity               = item.Quantity,
                TotalPrice             = item.TotalPrice,
                Note                   = item.Note,
                InvoiceNo              = item.InvoiceNo,
                FileName               = item.FileName,
                FileUrl                = fileUrl,
                InvoiceDate            = item.InvoiceDate,
                SortOrder              = item.SortOrder > 0 ? item.SortOrder : idx,
            });
        }

        return result;
    }
}
