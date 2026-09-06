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
    IAdvanceRequestReadService advanceReader,
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

        var heads = await db.AdvanceRequests
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
                a.AdvanceDate,
                a.AdvanceNeededDate,
                a.CashTotal,
                a.CheckTotal,
                a.GrandTotal,
                // 已沖銷金額只認「已核准」，與詳情頁 / 差額撥款（WriteOffRefundCalculator）同一基準，
                // 避免同一張預支單在表單頁與詳情頁顯示不同餘額
                WrittenOffTotal = db.WriteOffRecords
                    .Where(w => w.AdvanceRequestId == a.Id && w.ApprovalStatus == "approved")
                    .Sum(w => (decimal?)w.GrandTotal) ?? 0m,
                // 草稿 / 簽核中的沖銷金額另外帶出，供表單提示「另有 N 元沖銷中」，防止重複沖同一批費用
                PendingWriteOffTotal = db.WriteOffRecords
                    .Where(w => w.AdvanceRequestId == a.Id
                             && (w.ApprovalStatus == "draft" || w.ApprovalStatus == "pending" || w.ApprovalStatus == "returned"))
                    .Sum(w => (decimal?)w.GrandTotal) ?? 0m,
            })
            .ToListAsync();

        // 一次撈回全部明細與追加批次（不逐單查詢，避免 N+1）
        var ids = heads.Select(h => h.Id).ToList();

        var itemLookup = (await db.AdvanceRequestItems
                .AsNoTracking()
                .Where(i => ids.Contains(i.AdvanceRequestId))
                .OrderBy(i => i.RoundNo).ThenBy(i => i.SortOrder).ThenBy(i => i.Id)
                .ToListAsync())
            .ToLookup(i => i.AdvanceRequestId,
                      i => new AdvanceRequestItemDto(
                          i.Id, i.Category, i.SeqNo, i.ItemName, i.UnitPrice, i.Quantity,
                          i.TotalPrice, i.CashAmount, i.CheckAmount, i.Note, i.SortOrder,
                          i.FileName, i.FileUrl, i.RoundNo));

        var supLookup = (await db.AdvanceRequestSupplements
                .AsNoTracking()
                .Where(s => ids.Contains(s.AdvanceRequestId))
                .OrderBy(s => s.RoundNo)
                .Select(s => new { s.AdvanceRequestId, s.RoundNo, s.AdvanceDate, s.AdvanceNeededDate, s.Reason })
                .ToListAsync())
            .ToLookup(s => s.AdvanceRequestId);

        var list = heads.Select(h =>
        {
            var items = itemLookup[h.Id].ToArray();
            return new AvailableAdvanceDto(
                // 只撈 approved 的預支單，必定已於送簽時取號
                h.Id, h.RequestNo!, h.ProjectCode, h.ActivityName, h.AdvanceDate,
                h.CashTotal, h.CheckTotal, h.GrandTotal, h.WrittenOffTotal, h.PendingWriteOffTotal,
                // 批次組裝規則與 GET /advance-requests/{id} 共用同一份實作
                AdvanceRequestReadService.BuildRounds(h.AdvanceDate, h.AdvanceNeededDate, supLookup[h.Id], items),
                items);
        }).ToList();

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

    // ── 依預支單彙總檢視（預支單完整資訊 + 該單全部沖銷單）──────────────────

    public async Task<IActionResult> GetByAdvanceAsync(HttpRequest req, string advanceId)
    {
        var userId = await GetUserIdAsync(req);

        if (!int.TryParse(advanceId, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advance request ID format."));

        var advance = await advanceReader.GetByIdAsync(intId);
        if (advance is null)
            return new NotFoundObjectResult(ApiResponse.Fail("Advance request not found."));

        // 可見性比照單筆沖銷：預支單申請人、或該預支單底下任一沖銷單的申請人 / 審核者 / 指定審核者
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.IsSuperAdmin != true)
        {
            var writeOffIds = await db.WriteOffRecords.AsNoTracking()
                .Where(w => w.AdvanceRequestId == intId)
                .Select(w => w.Id)
                .ToListAsync();

            bool isAdvanceSubmitter = await db.AdvanceRequests.AsNoTracking()
                .AnyAsync(a => a.Id == intId && a.SubmittedById == userId);
            bool isSubmitter = await db.WriteOffRecords.AsNoTracking()
                .AnyAsync(w => w.AdvanceRequestId == intId && w.SubmittedById == userId);
            bool hasReviewed = await db.ApprovalRecords.AsNoTracking()
                .AnyAsync(ar => ar.ApplicationType == RequestType && writeOffIds.Contains(ar.ApplicationId) && ar.ReviewedById == userId);
            bool isDesignated = await db.RequestDesignatedReviewers.AsNoTracking()
                .AnyAsync(r => r.RequestType == RequestType && writeOffIds.Contains(r.RequestId) && r.ReviewerId == userId);

            if (!isAdvanceSubmitter && !isSubmitter && !hasReviewed && !isDesignated)
                return new NotFoundObjectResult(ApiResponse.Fail("Advance request not found."));
        }

        var writeOffs = await reader.GetByAdvanceIdAsync(intId);
        return new OkObjectResult(ApiResponse.Ok(new AdvanceWriteOffOverviewDto(advance, writeOffs)));
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
        // 可沖銷者＝預支單申請人本人，或 Superadmin（與 GetAvailableAdvancesAsync 的下拉清單範圍一致，
        // 否則 Superadmin 選得到別人的預支單卻在送出時吃 404）
        var isSuperAdmin = await db.Users.AsNoTracking()
            .AnyAsync(u => u.Id == submittedById && u.IsSuperAdmin);
        var ar = await db.AdvanceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == advanceRequestId
                                   && (isSuperAdmin || x.SubmittedById == submittedById))
            ?? throw AppException.NotFound("AdvanceRequest");

        EnsureAdvanceWriteOffable(ar);

        // 解析沖銷明細
        if (string.IsNullOrWhiteSpace(itemsJson))
            return new BadRequestObjectResult(ApiResponse.Fail("items 欄位為必填。"));

        var items = JsonSerializer.Deserialize<WriteOffItemMetadata[]>(itemsJson, JsonOpts);
        if (items is null || items.Length == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("At least one write-off item is required."));

        // 年份合理性（擋民國年誤植）
        RequestDateGuard.EnsureEach(items, i => i.InvoiceDate, "發票日期");

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

        var today = Clock.Now;

        // 上傳檔案至 Blob Storage，組裝沖銷明細
        var files         = form.Files.GetFiles("files");
        var writeOffItems = await BuildWriteOffItemsAsync(items, files);

        var wo = new WriteOffRecord
        {
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

        // 沖銷草稿可能是在預支追加之前建立的，編輯時需重新確認來源預支單仍可沖銷
        await EnsureAdvanceWriteOffableAsync(wo.AdvanceRequestId);

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

            // 年份合理性（擋民國年誤植，比照 CreateAsync）
            RequestDateGuard.EnsureEach(items, i => i.InvoiceDate, "發票日期");

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

        // 送簽時才取號：單號日期＝送簽日，草稿不佔號。
        // 退回（returned）重送時已有單號，不可重新配號，否則已流通的單號會被改掉。
        if (string.IsNullOrEmpty(wo.RequestNo))
            wo.RequestNo = await RequestNoGenerator.NextAsync(
                db.WriteOffRecords.Select(x => x.RequestNo), "WO-", Clock.Now);

        // 送簽日期只在首次送簽寫入：退回（returned）重送不改，與單號規則一致。
        wo.SubmittedAt ??= Clock.Now;

        // 送簽當下重新確認來源預支單仍可沖銷（避免對追加簽核中、總額變動中的預支單沖銷）
        await EnsureAdvanceWriteOffableAsync(wo.AdvanceRequestId);

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
        await DesignatedReviewerHelper.ValidateAndNormalizeAsync(db, RequestType, wo.Id, wo.ApprovalItemId, userId);
        await db.SaveChangesAsync();

        // 查詢指定審核者清單傳給 ResolveStartingStepAsync（含 ApprovalStepOrder 綁定步驟）
        var designatedReviewers = await DesignatedReviewerHelper.ReadForFlowAsync(db, RequestType, wo.Id);

        // 自審跳過邏輯（與請款、預支一致，自審不升級）；
        // 上層級關卡在同部門找不到更高階者時，改由上層部門主管接手（escalation）
        var (startStep, autoApproved, escalation) = await approvalFlow.ResolveStartingStepAsync(
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

        // 升級審核：上層級步驟在同部門找不到更高階審核者，改由上層部門主管接手
        if (escalation is not null)
        {
            db.EscalationOverrides.Add(new EscalationOverride
            {
                ApplicationType  = RequestType,
                ApplicationId    = wo.Id,
                StepOrder        = startStep,
                ReviewerId       = escalation.ReviewerId,
                OnBehalfOfUserId = escalation.OnBehalfOfUserId,
                CreatedAt        = Clock.Now,
            });
        }

        await db.SaveChangesAsync();

        // 通知審核者：指定審核步驟通知第一位指定審核者，否則通知符合條件的審核者
        if (!autoApproved && wo.SubmittedById.HasValue)
        {
            // 指定審核步驟（原生 UseApplicantDesignated 或例外指定審核命中）：讀 designee 快照，
            // 與 ResolveStartingStepAsync 的判定同源，確保不會誤走部門/職稱通知
            bool isDesignatedStep = designatedReviewers.Any(r => r.ApprovalStepOrder == startStep);

            if (escalation is not null)
                await notifier.NotifySpecificReviewerAsync(RequestType, wo.Id, escalation.ReviewerId,
                    wo.SubmittedById.Value, escalation.OnBehalfOfUserId is not null);
            else if (isDesignatedStep)
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

    // ── 差額撥款分期 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 已核准的沖銷單設定 / 修改差額撥款明細（僅財務體系部門或 Superadmin）。
    /// 應撥總額 = 本次沖銷造成的超支增額（<see cref="WriteOffRefundCalculator"/>），未超支則不可設定。
    /// 核准「當下」的寫入走 PATCH /approval-tasks/write_off/{id}/review，兩者共用 InstallmentUpsertService。
    /// </summary>
    public async Task<IActionResult> UpsertInstallmentsAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid ID format."));

        var userId = await GetUserIdAsync(req);

        var user = await db.Users.AsNoTracking().Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return new UnauthorizedObjectResult(ApiResponse.Fail("User not found."));
        if (!user.IsSuperAdmin && !DepartmentCodes.FinancialAndAbove.Contains(user.Department?.Code ?? ""))
            throw AppException.Forbidden("僅財務體系部門或 Superadmin 可設定撥款明細。");

        var wo = await db.WriteOffRecords
                         .Include(w => w.Installments)
                         .FirstOrDefaultAsync(w => w.Id == intId)
                 ?? throw AppException.NotFound("WriteOffRecord");

        if (wo.ApprovalStatus != "approved")
            return new BadRequestObjectResult(ApiResponse.Fail("只有已核准的沖銷申請可以設定撥款明細。"));

        var refundDue = await WriteOffRefundCalculator.CalculateAsync(db, wo);
        if (refundDue <= 0)
            return new BadRequestObjectResult(ApiResponse.Fail("本次沖銷未超過預支金額，無需撥款。"));

        var body = await req.ReadFromJsonAsync<UpsertInstallmentsRequest>(JsonOpts);
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        // 共用 validate + diff（不 SaveChanges）
        var newlyPaid = InstallmentUpsertService.Apply(
            db, wo.Installments, body.Installments, refundDue, userId,
            () => new WriteOffInstallment { WriteOffRecordId = wo.Id });

        await db.SaveChangesAsync();

        if (wo.SubmittedById.HasValue)
            foreach (var np in newlyPaid)
                await notifier.NotifyApplicantPaidAsync(
                    RequestType, wo.Id, wo.SubmittedById.Value, np.Amount, np.PaidAt,
                    installmentNo: np.InstallmentNo, totalInstallments: np.TotalInstallments);

        return new OkObjectResult(ApiResponse.Ok(
            new { wo.Id, InstallmentCount = body.Installments.Count },
            $"已更新 {body.Installments.Count} 筆撥款明細。"));
    }

    // ── 支票已支付註記 ───────────────────────────────────────────────────────

    /// <summary>
    /// 勾選 / 取消沖銷明細的「支票金額已支付」（僅財務管理部 <see cref="DepartmentCodes.FinanceStep"/> 或 Superadmin，
    /// 與撥款日 / 撥款明細 / 結案的判定範圍一致，刻意不含總監室 / 會計室）。
    /// 支票由公司直接付給廠商，不走撥款分期，僅以此旗標註記已付出。
    /// </summary>
    public async Task<IActionResult> UpdateCheckPaymentsAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid ID format."));

        var userId = await GetUserIdAsync(req);

        var user = await db.Users.AsNoTracking().Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return new UnauthorizedObjectResult(ApiResponse.Fail("User not found."));
        if (!user.IsSuperAdmin && !DepartmentCodes.FinanceStep.Contains(user.Department?.Code ?? ""))
            throw AppException.Forbidden("僅財務管理部或 Superadmin 可註記支票支付狀態。");

        var wo = await db.WriteOffRecords
                         .Include(w => w.Items)
                         .FirstOrDefaultAsync(w => w.Id == intId)
                 ?? throw AppException.NotFound("WriteOffRecord");

        if (wo.ApprovalStatus is not ("pending" or "approved"))
            return new BadRequestObjectResult(ApiResponse.Fail("只有待審核或已核准的沖銷申請可以註記支票支付狀態。"));

        var body = await req.ReadFromJsonAsync<UpdateCheckPaymentsRequest>(JsonOpts);
        if (body is null || body.Items.Length == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        foreach (var input in body.Items)
        {
            var item = wo.Items.FirstOrDefault(i => i.Id == input.ItemId)
                       ?? throw AppException.BadRequest($"找不到沖銷明細 Id={input.ItemId}。");

            if (input.CheckPaid && item.CheckAmount <= 0)
                throw AppException.BadRequest($"第 {item.SeqNo} 筆明細沒有支票金額，無法註記已支付。");

            item.CheckPaid     = input.CheckPaid;
            item.CheckPaidAt   = input.CheckPaid ? Clock.Now : null;
            item.CheckPaidById = input.CheckPaid ? userId : null;
        }

        await db.SaveChangesAsync();

        var paidCount = wo.Items.Count(i => i.CheckPaid);
        return new OkObjectResult(ApiResponse.Ok(
            new { wo.Id, PaidCount = paidCount },
            $"已更新支票支付狀態（已支付 {paidCount} 筆）。"));
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// 來源預支單是否可沖銷：須已核准且未結案。
    /// 追加簽核中的預支單狀態為 pending / returned，總額仍在變動，禁止沖銷。
    /// </summary>
    private static void EnsureAdvanceWriteOffable(AdvanceRequest ar)
    {
        if (ar.CurrentRoundNo > 1 && (ar.ApprovalStatus == "pending" || ar.ApprovalStatus == "returned"))
            throw AppException.BadRequest("此預支申請有進行中的追加批次，追加核准後才可沖銷。");

        if (ar.ApprovalStatus != "approved")
            throw AppException.BadRequest("Only approved advance requests can have write-offs.");

        if (ar.IsClosed)
            throw AppException.BadRequest("此預支申請已結案，無法再沖銷。");
    }

    private async Task EnsureAdvanceWriteOffableAsync(int advanceRequestId)
    {
        var ar = await db.AdvanceRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == advanceRequestId)
                 ?? throw AppException.NotFound("AdvanceRequest");
        EnsureAdvanceWriteOffable(ar);
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
        // 已拒絕的沖銷單必須排除，否則其發票號碼會被永久占用，申請人重開一張新單就卡 409 且無從自救
        // （與下方 InvoiceItems 的 ApprovalStatus != "rejected" 同一規則）
        var writeOffQuery = db.WriteOffItems
            .Where(wi => invoiceNos.Contains(wi.InvoiceNo!)
                      && wi.WriteOffRecord.ApprovalStatus != "rejected");

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
