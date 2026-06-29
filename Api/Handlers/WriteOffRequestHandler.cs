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

public sealed class WriteOffRequestHandler(
    AppDbContext db,
    IWriteOffRequestReadService reader,
    IBlobStorageService blob,
    IJwtService jwtService,
    IApprovalNotificationService notifier,
    IApprovalFlowService approvalFlow)
{
    private const string ContainerName = "write-off-invoices";
    private const string RequestType   = "write_off";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>沖銷明細 multipart JSON 的內部結構</summary>
    private sealed record WriteOffItemMetadata(
        string    Category,
        int       SeqNo,
        string    ItemName,
        decimal   UnitPrice,
        string    Quantity,
        decimal   TotalPrice,
        decimal   CashAmount,
        decimal   CheckAmount,
        string?   Note,
        string?   InvoiceNo,
        string?   FileName,
        string?   FileUrl,
        int       FileIndex,
        int       SortOrder,
        DateTime? InvoiceDate = null);

    // ── 可沖銷的預支申請清單（已核准且已撥款）───────────────────────────────

    public async Task<IActionResult> GetAvailableAdvancesAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        bool isSuperAdmin = user?.IsSuperAdmin == true;

        var list = await db.AdvanceRequests
            .AsNoTracking()
            .Include(a => a.Project)
            .Where(a => a.ApprovalStatus == "approved"
                     && !a.IsClosed
                     && (isSuperAdmin || a.SubmittedById == userId))
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.RequestNo,
                ProjectCode = a.Project != null ? a.Project.Code : "",
                a.ActivityName,
                a.GrandTotal,
                WrittenOffTotal = db.WriteOffRecords
                    .Where(w => w.AdvanceRequestId == a.Id && w.ApprovalStatus != "rejected")
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
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid write-off request ID format."));

        if (!await db.WriteOffRecords.AnyAsync(x => x.Id == intId))
            return new NotFoundObjectResult(ApiResponse.Fail("Write-off request not found."));

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.IsSuperAdmin != true)
        {
            bool isSubmitter = await db.WriteOffRecords.AnyAsync(x => x.Id == intId && x.SubmittedById == userId);
            bool hasReviewed = await db.ApprovalRecords.AsNoTracking()
                .AnyAsync(ar => ar.ApplicationType == "write_off" && ar.ApplicationId == intId && ar.ReviewedById == userId);
            bool isDesignated = await db.RequestDesignatedReviewers.AsNoTracking()
                .AnyAsync(r => r.RequestType == "write_off" && r.RequestId == intId && r.ReviewerId == userId);

            if (!isSubmitter && !hasReviewed && !isDesignated)
                return new NotFoundObjectResult(ApiResponse.Fail("Write-off request not found."));
        }

        var item = await reader.GetByIdAsync(intId);
        return new OkObjectResult(ApiResponse.Ok(item));
    }

    // ── 新增（multipart/form-data，支援發票檔案上傳）──────────────────────

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var submittedById = await GetUserIdAsync(req);

        var form = await req.ReadFormAsync();
        var advanceRequestIdStr = form["advanceRequestId"].ToString();
        var itemsJson           = form["items"].ToString();
        var note                = form["note"].ToString();
        var designatedReviewersJson = form["designatedReviewers"].ToString();

        if (!int.TryParse(advanceRequestIdStr, out var advanceRequestId))
            return new BadRequestObjectResult(ApiResponse.Fail("advanceRequestId 欄位為必填且須為整數。"));

        // 驗證預支申請存在、已核准、已撥款
        var ar = await db.AdvanceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == advanceRequestId && x.SubmittedById == submittedById)
            ?? throw AppException.NotFound("AdvanceRequest");

        if (ar.ApprovalStatus != "approved")
            throw AppException.BadRequest("Only approved advance requests can have write-offs.");

        if (ar.IsClosed)
            throw AppException.BadRequest("此預支申請已結案，無法再沖銷。");

        // 解析沖銷明細
        if (string.IsNullOrWhiteSpace(itemsJson))
            return new BadRequestObjectResult(ApiResponse.Fail("items 欄位為必填。"));

        var items = JsonSerializer.Deserialize<WriteOffItemMetadata[]>(itemsJson, JsonOpts);
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
        await ValidateInvoiceUniquenessAsync(items, excludeWriteOffRecordId: null);

        // 取得下一個沖銷編號（WriteOffNo：本預支單的第幾次沖銷）
        var lastNo = await db.WriteOffRecords
            .Where(w => w.AdvanceRequestId == advanceRequestId)
            .OrderByDescending(w => w.WriteOffNo)
            .Select(w => w.WriteOffNo)
            .FirstOrDefaultAsync();

        // 產生沖銷申請單號：WO-yyyyMMdd-NNN（唯一索引保護並發）
        var today  = Clock.Now;
        var prefix = $"WO-{today:yyyyMMdd}-";
        var maxNo  = await db.WriteOffRecords
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
        var files         = form.Files.GetFiles("files");
        var writeOffItems = await BuildWriteOffItemsAsync(items, files);

        var wo = new WriteOffRecord
        {
            RequestNo        = requestNo,
            AdvanceRequestId = advanceRequestId,
            WriteOffNo       = lastNo + 1,
            CashTotal        = items.Sum(i => i.CashAmount),
            CheckTotal       = items.Sum(i => i.CheckAmount),
            GrandTotal       = grandTotal,
            Note             = note,
            SubmittedById    = submittedById,
            ApprovalStatus   = "draft",
            CurrentStepOrder = 1,
            CreatedAt        = today,
        };
        wo.Items = writeOffItems;

        // 整單批次附件（照片 / PDF）
        var attachmentsJson = form["attachments"].ToString();
        if (!string.IsNullOrEmpty(attachmentsJson))
        {
            var attMetas    = JsonSerializer.Deserialize<AttachmentProcessor.AttachmentMetadata[]>(attachmentsJson, JsonOpts) ?? [];
            var attFiles    = form.Files.GetFiles("attachmentFiles");
            var resolvedAtt = await AttachmentProcessor.ResolveAsync(attMetas, attFiles, blob);
            wo.Attachments  = resolvedAtt.Select((a, i) => new WriteOffAttachment
            {
                FileName  = a.FileName,
                FileUrl   = a.FileUrl,
                SortOrder = i,
            }).ToList();
        }

        db.WriteOffRecords.Add(wo);
        await db.SaveChangesAsync();

        // 儲存指定審核者
        if (designatedReviewers is { Length: > 0 })
        {
            db.RequestDesignatedReviewers.AddRange(
                DesignatedReviewerHelper.BuildEntities(RequestType, wo.Id, designatedReviewers));
            await db.SaveChangesAsync();
        }

        var dto = await reader.GetByIdAsync(wo.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Write-off request created.")) { StatusCode = 201 };
    }

    // ── 更新草稿（multipart/form-data）──────────────────────────────────────

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);

        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid write-off request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var wo = currentUser?.IsSuperAdmin == true
            ? await db.WriteOffRecords.Include(x => x.Items).Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.WriteOffRecords.Include(x => x.Items).Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (wo is null) throw AppException.NotFound("WriteOffRecord");

        if (wo.ApprovalStatus != "draft" && wo.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned write-off requests can be edited.");

        var form    = await req.ReadFormAsync();
        var note    = form["note"].ToString();
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
                        DesignatedReviewerHelper.BuildEntities(RequestType, intId, updateDesignatedReviewers));
                }
            }
        }

        // 整單批次附件整組替換（提供 attachments 欄位才更新；blob 待主交易存檔後再清理）
        var removedAttachmentBlobs = new List<string>();
        if (form.ContainsKey("attachments"))
        {
            var attMetas    = JsonSerializer.Deserialize<AttachmentProcessor.AttachmentMetadata[]>(form["attachments"].ToString(), JsonOpts) ?? [];
            var attFiles    = form.Files.GetFiles("attachmentFiles");
            var oldAttUrls  = wo.Attachments.Where(a => !string.IsNullOrEmpty(a.FileUrl)).Select(a => a.FileUrl!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var resolvedAtt = await AttachmentProcessor.ResolveAsync(attMetas, attFiles, blob);
            var newAttUrls  = resolvedAtt.Where(a => !string.IsNullOrEmpty(a.FileUrl)).Select(a => a.FileUrl!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            db.WriteOffAttachments.RemoveRange(wo.Attachments);
            wo.Attachments = resolvedAtt.Select((a, i) => new WriteOffAttachment
            {
                FileName  = a.FileName,
                FileUrl   = a.FileUrl,
                SortOrder = i,
            }).ToList();
            removedAttachmentBlobs = oldAttUrls.Except(newAttUrls).ToList();
        }

        // 更新沖銷明細（提供時才更新）
        if (!string.IsNullOrWhiteSpace(itemsJson))
        {
            var items = JsonSerializer.Deserialize<WriteOffItemMetadata[]>(itemsJson, JsonOpts);
            if (items is null || items.Length == 0)
                return new BadRequestObjectResult(ApiResponse.Fail("At least one write-off item is required."));

            // 發票唯一性驗證（排除本筆記錄自身的明細）
            await ValidateInvoiceUniquenessAsync(items, excludeWriteOffRecordId: intId);

            var newGrandTotal = items.Sum(i => i.TotalPrice);

            // 收集舊 FileUrl（比對後刪除不再使用的 blob）
            var oldFileUrls = wo.Items
                .Where(i => !string.IsNullOrEmpty(i.FileUrl))
                .Select(i => i.FileUrl!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 上傳新檔案、保留既有 URL
            var files         = form.Files.GetFiles("files");
            var newWriteOffItems = new List<WriteOffItem>();
            var newFileUrls   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

                newWriteOffItems.Add(new WriteOffItem
                {
                    WriteOffRecordId = intId,
                    Category         = item.Category,
                    SeqNo            = item.SeqNo,
                    ItemName         = item.ItemName,
                    UnitPrice        = item.UnitPrice,
                    Quantity         = item.Quantity,
                    TotalPrice       = item.TotalPrice,
                    CashAmount       = item.CashAmount,
                    CheckAmount      = item.CheckAmount,
                    Note             = item.Note,
                    InvoiceNo        = item.InvoiceNo,
                    FileName         = item.FileName,
                    FileUrl          = fileUrl,
                    InvoiceDate      = item.InvoiceDate,
                    SortOrder        = item.SortOrder > 0 ? item.SortOrder : idx,
                });
            }

            db.WriteOffItems.RemoveRange(wo.Items);
            wo.Items      = newWriteOffItems;
            wo.CashTotal  = items.Sum(i => i.CashAmount);
            wo.CheckTotal = items.Sum(i => i.CheckAmount);
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

        // 刪除不再使用的附件 blob（主交易存檔後才清理，避免孤兒資料）
        foreach (var url in removedAttachmentBlobs)
        {
            var blobName = blob.ExtractBlobName(url, AttachmentProcessor.ContainerName);
            if (blobName is not null)
                await blob.DeleteAsync(AttachmentProcessor.ContainerName, blobName);
        }

        var dto = await reader.GetByIdAsync(wo.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Write-off request updated."));
    }

    // ── 刪除（僅草稿）────────────────────────────────────────────────────────

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);

        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid write-off request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var wo = currentUser?.IsSuperAdmin == true
            ? await db.WriteOffRecords.Include(x => x.Items).Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.WriteOffRecords.Include(x => x.Items).Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (wo is null) throw AppException.NotFound("WriteOffRecord");

        if (wo.ApprovalStatus != "draft" && wo.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned write-off requests can be deleted.");

        // 收集要刪除的 blob（沖銷明細發票 + 整單附件）
        var blobNames = wo.Items
            .Select(i => blob.ExtractBlobName(i.FileUrl, ContainerName))
            .Where(n => n is not null)
            .ToList();
        var attachmentBlobNames = wo.Attachments
            .Select(a => blob.ExtractBlobName(a.FileUrl, AttachmentProcessor.ContainerName))
            .Where(n => n is not null)
            .ToList();

        // 一併清除此申請單的審核流程足跡（多型關聯無 FK，須手動刪除，否則殘留列會擋住使用者刪除）
        db.ApprovalRecords.RemoveRange(
            await db.ApprovalRecords.Where(r => r.ApplicationType == RequestType && r.ApplicationId == wo.Id).ToListAsync());
        db.EscalationOverrides.RemoveRange(
            await db.EscalationOverrides.Where(o => o.ApplicationType == RequestType && o.ApplicationId == wo.Id).ToListAsync());
        db.RequestDesignatedReviewers.RemoveRange(
            await db.RequestDesignatedReviewers.Where(r => r.RequestType == RequestType && r.RequestId == wo.Id).ToListAsync());

        db.WriteOffRecords.Remove(wo);
        await db.SaveChangesAsync();

        // 刪除 blob files（在 DB 刪除後才清理，避免孤兒資料）
        foreach (var name in blobNames)
            await blob.DeleteAsync(ContainerName, name!);
        foreach (var name in attachmentBlobNames)
            await blob.DeleteAsync(AttachmentProcessor.ContainerName, name!);

        return new OkObjectResult(ApiResponse.Ok($"Write-off request '{id}' deleted."));
    }

    // ── 送出申請（draft/returned → pending）──────────────────────────────────

    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);

        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid write-off request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var wo = currentUser?.IsSuperAdmin == true
            ? await db.WriteOffRecords.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.WriteOffRecords.FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (wo is null) throw AppException.NotFound("WriteOffRecord");

        if (wo.ApprovalStatus != "draft" && wo.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned write-off requests can be submitted.");

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

        var submitter = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

        // 自動關聯簽核流程（依申請人部門挑流程：部門專屬優先，否則退回通用預設）
        if (wo.ApprovalItemId is null)
            wo.ApprovalItemId = await approvalFlow.ResolveApprovalItemIdAsync(RequestType, submitter?.DepartmentId);

        // Superadmin 無部門歸屬，直接自動核准
        if (submitter?.IsSuperAdmin == true)
        {
            wo.ApprovalStatus   = "approved";
            wo.CurrentStepOrder = 1;
            wo.ReviewedAt       = Clock.Now;
            wo.ReviewedById     = userId;
            wo.ReviewNote       = "系統自動核准（Superadmin）";
            await db.SaveChangesAsync();
            var saDto = await reader.GetByIdAsync(wo.Id);
            return new OkObjectResult(ApiResponse.Ok(saDto, "Write-off request auto-approved."));
        }

        // 正規化各 designee 所屬步驟並驗證每個指定審核步驟皆有審核者
        await DesignatedReviewerHelper.ValidateAndNormalizeAsync(db, RequestType, wo.Id, wo.ApprovalItemId);
        await db.SaveChangesAsync();

        // 查詢指定審核者清單傳給 ResolveStartingStepAsync（含 ApprovalStepOrder 綁定步驟）
        var designatedReviewers = await DesignatedReviewerHelper.ReadForFlowAsync(db, RequestType, wo.Id);

        // 自審跳過邏輯（與請款、預支一致，不升級）
        var (startStep, autoApproved, _) = await approvalFlow.ResolveStartingStepAsync(
            wo.ApprovalItemId, userId, RequestType, designatedReviewers);

        if (autoApproved)
        {
            wo.ApprovalStatus   = "approved";
            wo.CurrentStepOrder = startStep;
            wo.ReviewedAt       = Clock.Now;
            wo.ReviewedById     = userId;
            wo.ReviewNote       = "系統自動核准（所有審核步驟皆為申請人本人）";
        }
        else
        {
            wo.ApprovalStatus   = "pending";
            wo.CurrentStepOrder = startStep;
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
                    .Where(r => r.RequestType == RequestType && r.RequestId == wo.Id
                             && r.ApprovalStepOrder == startStep && r.Status == "pending")
                    .OrderBy(r => r.StepOrder)
                    .FirstOrDefaultAsync();
                if (firstReviewer is not null)
                    await notifier.NotifySpecificReviewerAsync(RequestType, wo.Id, firstReviewer.ReviewerId, wo.SubmittedById.Value, false);
            }
            else
                await notifier.NotifyReviewersAsync(RequestType, wo.Id, wo.ApprovalItemId, startStep, wo.SubmittedById.Value);
        }

        var dto = await reader.GetByIdAsync(wo.Id);
        var msg = autoApproved ? "Write-off request auto-approved." : "Write-off request submitted.";
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
    /// 驗證發票號碼唯一性：批次內去重 + 跨所有沖銷與請款發票表（排除已拒絕申請）。
    /// excludeWriteOffRecordId：更新時傳入自身 ID 以排除自身明細。
    /// </summary>
    private async Task ValidateInvoiceUniquenessAsync(
        WriteOffItemMetadata[] items,
        int? excludeWriteOffRecordId)
    {
        // 發票號碼含中文 / CJK 者（如「收據」「領據」）視為手打文字，排除於重複檢查之外
        var invoiceNos = items
            .Where(i => !string.IsNullOrWhiteSpace(i.InvoiceNo)
                     && !InvoiceNoHelper.IsManualText(i.InvoiceNo))
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

        // 資料庫唯一性檢查（跨所有沖銷 + 請款發票，排除已拒絕的申請）
        var writeOffQuery = db.WriteOffItems
            .Where(wi => invoiceNos.Contains(wi.InvoiceNo!));

        // 更新場景：排除本筆 WriteOffRecord 的明細
        if (excludeWriteOffRecordId.HasValue)
            writeOffQuery = writeOffQuery.Where(wi => wi.WriteOffRecordId != excludeWriteOffRecordId.Value);

        var existInWriteOff = await writeOffQuery
            .Select(wi => wi.InvoiceNo!)
            .Distinct()
            .ToListAsync();

        var existInInvoice = await db.InvoiceItems
            .Where(ii => invoiceNos.Contains(ii.InvoiceNo)
                      && ii.PaymentRequest.ApprovalStatus != "rejected")
            .Select(ii => ii.InvoiceNo)
            .Distinct()
            .ToListAsync();

        var existingNos = existInWriteOff.Union(existInInvoice).Distinct().ToList();
        if (existingNos.Count > 0)
            throw AppException.Conflict($"發票號碼已存在：{string.Join(", ", existingNos)}");
    }

    /// <summary>
    /// 依 multipart metadata 與上傳檔案清單，組裝 WriteOffItem 清單並上傳至 Blob Storage。
    /// </summary>
    private async Task<List<WriteOffItem>> BuildWriteOffItemsAsync(
        WriteOffItemMetadata[] items,
        IReadOnlyList<IFormFile> files)
    {
        var result = new List<WriteOffItem>(items.Length);

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

            result.Add(new WriteOffItem
            {
                Category    = item.Category,
                SeqNo       = item.SeqNo,
                ItemName    = item.ItemName,
                UnitPrice   = item.UnitPrice,
                Quantity    = item.Quantity,
                TotalPrice  = item.TotalPrice,
                CashAmount  = item.CashAmount,
                CheckAmount = item.CheckAmount,
                Note        = item.Note,
                InvoiceNo   = item.InvoiceNo,
                FileName    = item.FileName,
                FileUrl     = fileUrl,
                InvoiceDate = item.InvoiceDate,
                SortOrder   = item.SortOrder > 0 ? item.SortOrder : idx,
            });
        }

        return result;
    }
}
