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

public sealed class PreReviewRequestHandler(
    AppDbContext db,
    IPreReviewRequestReadService reader,
    IBlobStorageService blob,
    IJwtService jwtService,
    IApprovalNotificationService notifier,
    IApprovalFlowService approvalFlow)
{
    private static readonly HashSet<string> ValidTypes = ["vendor", "designer"];
    private const string ContainerName = "quotes";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Multipart form 中 items JSON 的內部結構</summary>
    private sealed record ItemMetadata(
        string    FileName,
        string?   ItemCategory,
        decimal   Amount,
        string?   ItemName,
        string?   Description,
        string?   Note,
        string?   FileUrl,
        int       FileIndex,
        DateTime? ItemDate = null);

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
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid pre-review request ID format."));

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var exists = user?.IsSuperAdmin == true
            ? await db.PreReviewRequests.AnyAsync(x => x.Id == intId)
            : await db.PreReviewRequests.AnyAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (!exists)
            return new NotFoundObjectResult(ApiResponse.Fail("Pre-review request not found."));

        var item = await reader.GetByIdAsync(intId);
        return new OkObjectResult(ApiResponse.Ok(item));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        // SubmittedById 由 JWT 中的 sub claim 決定，不信任客戶端傳入的值
        var submittedById = await GetUserIdAsync(req);

        var form = await req.ReadFormAsync();

        var type        = form["type"].ToString();
        var projectId   = int.TryParse(form["projectId"], out var pid) ? pid : 0;
        var reason      = form["reason"].ToString();
        var itemsJson   = form["items"].ToString();
        var taxAmount   = decimal.TryParse(form["taxAmount"], out var ta) ? ta : 0m;
        var designatedReviewersJson = form["designatedReviewers"].ToString();
        var vendorIdStr = form["vendorId"].ToString();
        int? vendorId = int.TryParse(vendorIdStr, out var vid) ? vid : null;
        DesignatedReviewerRequest[]? designatedReviewers = null;
        if (!string.IsNullOrEmpty(designatedReviewersJson))
            designatedReviewers = JsonSerializer.Deserialize<DesignatedReviewerRequest[]>(designatedReviewersJson, JsonOpts);

        if (string.IsNullOrEmpty(type) || !ValidTypes.Contains(type))
            return new BadRequestObjectResult(ApiResponse.Fail($"Invalid type '{type}'. Must be vendor or designer."));

        if (!await db.Projects.AnyAsync(p => p.Id == projectId))
            throw AppException.NotFound("Project");

        // 協力廠商 / 設計師 皆須指定有效（且 IsActive）廠商
        if (!vendorId.HasValue)
            return new BadRequestObjectResult(ApiResponse.Fail("預審申請必須指定廠商。"));
        var vendor = await db.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vendorId.Value);
        if (vendor is null)
            return new NotFoundObjectResult(ApiResponse.Fail("指定的廠商不存在。"));
        if (!vendor.IsActive)
            return new BadRequestObjectResult(ApiResponse.Fail("此廠商已停用，無法選用。"));

        // 指定審核者存在性驗證
        if (designatedReviewers is { Length: > 0 })
        {
            var reviewerIds = designatedReviewers.Select(r => r.ReviewerId).Distinct().ToList();
            var existCount = await db.Users.AsNoTracking().CountAsync(u => reviewerIds.Contains(u.Id));
            if (existCount != reviewerIds.Count)
                return new BadRequestObjectResult(ApiResponse.Fail("一或多位指定審核者不存在。"));
        }

        var items = JsonSerializer.Deserialize<ItemMetadata[]>(itemsJson, JsonOpts);
        if (items is null || items.Length == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("At least one item is required."));

        // 產生預審申請單號：PRV-yyyyMMdd-NNN（唯一索引保護並發）
        var today = Clock.Now;
        var prefix = $"PRV-{today:yyyyMMdd}-";
        var maxNo = await db.PreReviewRequests
            .Where(p => p.RequestNo.StartsWith(prefix))
            .MaxAsync(p => (string?)p.RequestNo);
        int seq = 1;
        if (maxNo is not null)
        {
            var seqStr = maxNo[prefix.Length..];
            if (int.TryParse(seqStr, out var parsed))
                seq = parsed + 1;
        }
        var requestNo = $"{prefix}{seq:D3}";

        // 上傳檔案至 Blob Storage（quotes 容器）
        var files = form.Files.GetFiles("files");
        var reviewItems = new List<PreReviewItem>();
        foreach (var item in items)
        {
            string? fileUrl = null;
            if (item.FileIndex >= 0 && item.FileIndex < files.Count)
            {
                var file = files[item.FileIndex];
                var ext = Path.GetExtension(file.FileName);
                var blobName = $"{Clock.Now:yyyy/MM}/{Guid.NewGuid()}{ext}";
                using var stream = file.OpenReadStream();
                fileUrl = await blob.UploadAsync(ContainerName, blobName, stream, file.ContentType);
            }

            reviewItems.Add(new PreReviewItem
            {
                FileName     = item.FileName,
                ItemCategory = item.ItemCategory,
                Amount       = item.Amount,
                ItemName     = item.ItemName,
                Description  = item.Description,
                Note         = item.Note,
                FileUrl      = fileUrl,
                ItemDate     = item.ItemDate,
            });
        }

        var pr = new PreReviewRequest
        {
            RequestNo     = requestNo,
            Type          = type,
            ProjectId     = projectId,
            VendorId      = vendorId,
            Reason        = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            SubmittedById = submittedById,
            TotalAmount   = items.Sum(i => i.Amount),
            TaxAmount     = taxAmount,
            ApprovalStatus = "draft",
            CreatedAt     = today,
        };
        pr.Items = reviewItems;

        // 整單批次附件
        var attachmentsJson = form["attachments"].ToString();
        if (!string.IsNullOrEmpty(attachmentsJson))
        {
            var attMetas    = JsonSerializer.Deserialize<AttachmentProcessor.AttachmentMetadata[]>(attachmentsJson, JsonOpts) ?? [];
            var attFiles    = form.Files.GetFiles("attachmentFiles");
            var resolvedAtt = await AttachmentProcessor.ResolveAsync(attMetas, attFiles, blob);
            pr.Attachments  = resolvedAtt.Select((a, i) => new PreReviewRequestAttachment
            {
                FileName  = a.FileName,
                FileUrl   = a.FileUrl,
                SortOrder = i,
            }).ToList();
        }

        db.PreReviewRequests.Add(pr);
        await db.SaveChangesAsync();

        // 儲存指定審核者
        if (designatedReviewers is { Length: > 0 })
        {
            db.RequestDesignatedReviewers.AddRange(
                DesignatedReviewerHelper.BuildEntities("pre_review", pr.Id, designatedReviewers));
            await db.SaveChangesAsync();
        }

        var dto = await reader.GetByIdAsync(pr.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Pre-review request created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid pre-review request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var pr = currentUser?.IsSuperAdmin == true
            ? await db.PreReviewRequests.Include(x => x.Items).Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.PreReviewRequests.Include(x => x.Items).Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (pr is null) throw AppException.NotFound("PreReviewRequest");

        if (pr.ApprovalStatus != "draft" && pr.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned pre-review requests can be edited.");

        var form = await req.ReadFormAsync();

        var type      = form["type"].ToString();
        var projectId = int.TryParse(form["projectId"], out var pid) ? pid : (int?)null;
        var reason    = form["reason"].ToString();
        if (form.ContainsKey("taxAmount") && decimal.TryParse(form["taxAmount"], out var ta))
            pr.TaxAmount = ta;
        var updateDesignatedReviewersJson = form["designatedReviewers"].ToString();
        DesignatedReviewerRequest[]? updateDesignatedReviewers = null;
        // 空字串表示「不更新」，非空字串（包含 "[]"）才處理
        if (!string.IsNullOrEmpty(updateDesignatedReviewersJson))
            updateDesignatedReviewers = JsonSerializer.Deserialize<DesignatedReviewerRequest[]>(updateDesignatedReviewersJson, JsonOpts);

        if (!string.IsNullOrEmpty(type))
        {
            if (!ValidTypes.Contains(type))
                return new BadRequestObjectResult(ApiResponse.Fail($"Invalid type '{type}'."));
            pr.Type = type;
        }

        // 更新申請原因（空字串表示清除）
        if (form.ContainsKey("reason"))
            pr.Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        if (projectId.HasValue)
        {
            if (!await db.Projects.AnyAsync(p => p.Id == projectId))
                throw AppException.NotFound("Project");
            pr.ProjectId = projectId.Value;
        }

        // 廠商驗證與寫入
        if (form.ContainsKey("vendorId") || form.ContainsKey("type"))
        {
            var vendorIdStr = form["vendorId"].ToString();
            int? vendorId = int.TryParse(vendorIdStr, out var vid) ? vid : null;

            // 協力廠商 / 設計師 皆須指定有效廠商
            if (!vendorId.HasValue)
                return new BadRequestObjectResult(ApiResponse.Fail("預審申請必須指定廠商。"));
            var vendor = await db.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vendorId.Value);
            if (vendor is null)
                return new NotFoundObjectResult(ApiResponse.Fail("指定的廠商不存在。"));
            if (!vendor.IsActive && pr.VendorId != vendorId)
                return new BadRequestObjectResult(ApiResponse.Fail("此廠商已停用，無法選用。"));
            pr.VendorId = vendorId;
        }

        // 指定審核者整組替換（提供時才更新）
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
                .Where(r => r.RequestType == "pre_review" && r.RequestId == intId)
                .ToListAsync();
            db.RequestDesignatedReviewers.RemoveRange(old);
            if (updateDesignatedReviewers.Length > 0)
            {
                db.RequestDesignatedReviewers.AddRange(
                    DesignatedReviewerHelper.BuildEntities("pre_review", intId, updateDesignatedReviewers));
            }
        }

        // 整單批次附件整組替換（提供 attachments 欄位才更新）
        var removedAttachmentBlobs = new List<string>();
        if (form.ContainsKey("attachments"))
        {
            var attMetas    = JsonSerializer.Deserialize<AttachmentProcessor.AttachmentMetadata[]>(form["attachments"].ToString(), JsonOpts) ?? [];
            var attFiles    = form.Files.GetFiles("attachmentFiles");
            var oldAttUrls  = pr.Attachments.Where(a => !string.IsNullOrEmpty(a.FileUrl)).Select(a => a.FileUrl!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var resolvedAtt = await AttachmentProcessor.ResolveAsync(attMetas, attFiles, blob);
            var newAttUrls  = resolvedAtt.Where(a => !string.IsNullOrEmpty(a.FileUrl)).Select(a => a.FileUrl!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            db.PreReviewRequestAttachments.RemoveRange(pr.Attachments);
            pr.Attachments = resolvedAtt.Select((a, i) => new PreReviewRequestAttachment
            {
                FileName  = a.FileName,
                FileUrl   = a.FileUrl,
                SortOrder = i,
            }).ToList();
            removedAttachmentBlobs = oldAttUrls.Except(newAttUrls).ToList();
        }

        var itemsJson = form["items"].ToString();
        if (!string.IsNullOrEmpty(itemsJson))
        {
            var items = JsonSerializer.Deserialize<ItemMetadata[]>(itemsJson, JsonOpts);
            if (items is null || items.Length == 0)
                return new BadRequestObjectResult(ApiResponse.Fail("At least one item is required."));

            // 收集舊 FileUrl（稍後比對，刪除不再使用的 blob）
            var oldFileUrls = pr.Items
                .Where(ii => !string.IsNullOrEmpty(ii.FileUrl))
                .Select(ii => ii.FileUrl!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 上傳新檔案、保留既有 URL
            var files = form.Files.GetFiles("files");
            var newFileUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var reviewItems = new List<PreReviewItem>();

            foreach (var item in items)
            {
                string? fileUrl = item.FileUrl; // 保留既有 URL
                if (item.FileIndex >= 0 && item.FileIndex < files.Count)
                {
                    var file = files[item.FileIndex];
                    var ext = Path.GetExtension(file.FileName);
                    var blobName = $"{Clock.Now:yyyy/MM}/{Guid.NewGuid()}{ext}";
                    using var stream = file.OpenReadStream();
                    fileUrl = await blob.UploadAsync(ContainerName, blobName, stream, file.ContentType);
                }
                if (!string.IsNullOrEmpty(fileUrl))
                    newFileUrls.Add(fileUrl);

                reviewItems.Add(new PreReviewItem
                {
                    FileName     = item.FileName,
                    ItemCategory = item.ItemCategory,
                    Amount       = item.Amount,
                    ItemName     = item.ItemName,
                    Description  = item.Description,
                    Note         = item.Note,
                    FileUrl      = fileUrl,
                    ItemDate     = item.ItemDate,
                });
            }

            db.PreReviewItems.RemoveRange(pr.Items);
            pr.Items       = reviewItems;
            pr.TotalAmount = items.Sum(i => i.Amount);

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

        // 刪除不再使用的附件 blob（主交易存檔後才清理）
        foreach (var url in removedAttachmentBlobs)
        {
            var blobName = blob.ExtractBlobName(url, AttachmentProcessor.ContainerName);
            if (blobName is not null)
                await blob.DeleteAsync(AttachmentProcessor.ContainerName, blobName);
        }

        var dto = await reader.GetByIdAsync(pr.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Pre-review request updated."));
    }

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid pre-review request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var pr = currentUser?.IsSuperAdmin == true
            ? await db.PreReviewRequests.Include(x => x.Items).Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.PreReviewRequests.Include(x => x.Items).Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (pr is null) throw AppException.NotFound("PreReviewRequest");

        if (pr.ApprovalStatus != "draft" && pr.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned pre-review requests can be deleted.");

        // 收集要刪除的 blob（品項附件 + 整單附件）
        var blobNames = pr.Items
            .Select(ii => blob.ExtractBlobName(ii.FileUrl, ContainerName))
            .Where(n => n is not null)
            .ToList();
        var attachmentBlobNames = pr.Attachments
            .Select(a => blob.ExtractBlobName(a.FileUrl, AttachmentProcessor.ContainerName))
            .Where(n => n is not null)
            .ToList();

        // 一併清除此申請單的審核流程足跡（多型關聯無 FK，須手動刪除）
        db.ApprovalRecords.RemoveRange(
            await db.ApprovalRecords.Where(r => r.ApplicationType == "pre_review" && r.ApplicationId == pr.Id).ToListAsync());
        db.EscalationOverrides.RemoveRange(
            await db.EscalationOverrides.Where(o => o.ApplicationType == "pre_review" && o.ApplicationId == pr.Id).ToListAsync());
        db.RequestDesignatedReviewers.RemoveRange(
            await db.RequestDesignatedReviewers.Where(r => r.RequestType == "pre_review" && r.RequestId == pr.Id).ToListAsync());

        db.PreReviewRequests.Remove(pr);
        await db.SaveChangesAsync();

        // 刪除 blob files
        foreach (var name in blobNames)
            await blob.DeleteAsync(ContainerName, name!);
        foreach (var name in attachmentBlobNames)
            await blob.DeleteAsync(AttachmentProcessor.ContainerName, name!);

        return new OkObjectResult(ApiResponse.Ok($"Pre-review request '{id}' deleted."));
    }

    /// <summary>送出申請（draft → pending）</summary>
    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid pre-review request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var pr = currentUser?.IsSuperAdmin == true
            ? await db.PreReviewRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.PreReviewRequests.FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (pr is null) throw AppException.NotFound("PreReviewRequest");

        if (pr.ApprovalStatus != "draft" && pr.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned pre-review requests can be submitted.");

        // 退回重送時清除舊審核記錄，重置指定審核者狀態，重新走流程
        if (pr.ApprovalStatus == "returned")
        {
            var oldRecords = await db.ApprovalRecords
                .Where(r => r.ApplicationType == "pre_review" && r.ApplicationId == pr.Id)
                .ToListAsync();
            db.ApprovalRecords.RemoveRange(oldRecords);

            var oldOverrides = await db.EscalationOverrides
                .Where(o => o.ApplicationType == "pre_review" && o.ApplicationId == pr.Id)
                .ToListAsync();
            db.EscalationOverrides.RemoveRange(oldOverrides);

            // 重置指定審核者狀態為 pending
            var rdrsToReset = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == "pre_review" && r.RequestId == pr.Id)
                .ToListAsync();
            foreach (var rdr in rdrsToReset)
            {
                rdr.Status     = "pending";
                rdr.ReviewedAt = null;
                rdr.Comment    = null;
            }
        }

        var submitter = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

        // 自動關聯簽核流程（依申請人部門挑流程）
        if (pr.ApprovalItemId is null)
            pr.ApprovalItemId = await approvalFlow.ResolveApprovalItemIdAsync("pre_review", submitter?.DepartmentId);

        // Superadmin 無部門歸屬，直接自動核准
        if (submitter?.IsSuperAdmin == true)
        {
            pr.ApprovalStatus   = "approved";
            pr.CurrentStepOrder = 1;
            pr.ReviewedAt       = Clock.Now;
            pr.ReviewedById     = userId;
            pr.ReviewNote       = "系統自動核准（Superadmin）";
            await db.SaveChangesAsync();
            var saDto = await reader.GetByIdAsync(pr.Id);
            return new OkObjectResult(ApiResponse.Ok(saDto, "Pre-review request auto-approved."));
        }

        // 正規化各 designee 所屬步驟並驗證每個指定審核步驟皆有審核者
        await DesignatedReviewerHelper.ValidateAndNormalizeAsync(db, "pre_review", pr.Id, pr.ApprovalItemId);
        await db.SaveChangesAsync();

        // 查詢指定審核者清單傳給 ResolveStartingStepAsync（含 ApprovalStepOrder 綁定步驟）
        var designatedReviewers = await DesignatedReviewerHelper.ReadForFlowAsync(db, "pre_review", pr.Id);

        // 自動跳過「申請人即審核者」的步驟
        var (startStep, autoApproved, _) = await approvalFlow.ResolveStartingStepAsync(
            pr.ApprovalItemId, userId, "pre_review", designatedReviewers);

        if (autoApproved)
        {
            pr.ApprovalStatus   = "approved";
            pr.CurrentStepOrder = startStep;
            pr.ReviewedAt       = Clock.Now;
            pr.ReviewedById     = userId;
            pr.ReviewNote       = "系統自動核准（所有審核步驟皆為申請人本人）";
        }
        else
        {
            pr.ApprovalStatus   = "pending";
            pr.CurrentStepOrder = startStep;
        }

        await db.SaveChangesAsync();

        // 通知審核者
        if (!autoApproved && pr.SubmittedById.HasValue)
        {
            bool isDesignatedStep = pr.ApprovalItemId.HasValue && await db.ApprovalSteps.AsNoTracking()
                .AnyAsync(s => s.ApprovalItemId == pr.ApprovalItemId
                    && s.StepOrder == startStep
                    && s.UseApplicantDesignated);
            if (isDesignatedStep)
            {
                var firstReviewer = await db.RequestDesignatedReviewers
                    .AsNoTracking()
                    .Where(r => r.RequestType == "pre_review" && r.RequestId == pr.Id
                             && r.ApprovalStepOrder == startStep && r.Status == "pending")
                    .OrderBy(r => r.StepOrder)
                    .FirstOrDefaultAsync();
                if (firstReviewer is not null)
                    await notifier.NotifySpecificReviewerAsync("pre_review", pr.Id, firstReviewer.ReviewerId, pr.SubmittedById.Value, false);
            }
            else
                await notifier.NotifyReviewersAsync("pre_review", pr.Id, pr.ApprovalItemId, startStep, pr.SubmittedById.Value);
        }

        var dto = await reader.GetByIdAsync(pr.Id);
        var msg = autoApproved ? "Pre-review request auto-approved." : "Pre-review request submitted.";
        return new OkObjectResult(ApiResponse.Ok(dto, msg));
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
