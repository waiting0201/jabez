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
using System.Text.Json.Serialization;

namespace Jabez.Api.Handlers;

public sealed class PaymentRequestHandler(
    AppDbContext db,
    IPaymentRequestReadService reader,
    IBlobStorageService blob,
    IJwtService jwtService,
    IApprovalNotificationService notifier,
    IApprovalFlowService approvalFlow)
{
    private static readonly HashSet<string> ValidTypes = ["vendor", "general", "business_trip"];
    private const string ContainerName = "invoices";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Multipart form 中 invoices JSON 的內部結構</summary>
    private sealed record InvoiceMetadata(
        string    FileName,
        string    InvoiceNo,
        decimal   Amount,
        string?   ItemName,
        string?   Note,
        string?   FileUrl,
        int       FileIndex,
        DateTime? InvoiceDate = null);

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
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid payment request ID format."));

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var exists = user?.IsSuperAdmin == true
            ? await db.PaymentRequests.AnyAsync(x => x.Id == intId)
            : await db.PaymentRequests.AnyAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (!exists)
            return new NotFoundObjectResult(ApiResponse.Fail("Payment request not found."));

        var item = await reader.GetByIdAsync(intId);
        return new OkObjectResult(ApiResponse.Ok(item));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        // BUG-04: SubmittedById 由 JWT 中的 sub claim 決定，不信任客戶端傳入的值
        var submittedById = await GetUserIdAsync(req);

        var form = await req.ReadFormAsync();

        var type      = form["type"].ToString();
        var projectId = int.TryParse(form["projectId"], out var pid) ? pid : 0;
        var reason    = form["reason"].ToString();
        var invoicesJson = form["invoices"].ToString();
        var designatedReviewersJson = form["designatedReviewers"].ToString();
        var vendorIdStr = form["vendorId"].ToString();
        int? vendorId = int.TryParse(vendorIdStr, out var vid) ? vid : null;
        DesignatedReviewerRequest[]? designatedReviewers = null;
        if (!string.IsNullOrEmpty(designatedReviewersJson))
            designatedReviewers = JsonSerializer.Deserialize<DesignatedReviewerRequest[]>(designatedReviewersJson, JsonOpts);

        if (string.IsNullOrEmpty(type) || !ValidTypes.Contains(type))
            return new BadRequestObjectResult(ApiResponse.Fail($"Invalid type '{type}'. Must be vendor, travel, or advance."));

        if (string.IsNullOrWhiteSpace(reason))
            return new BadRequestObjectResult(ApiResponse.Fail("請填寫請款原因。"));

        if (!await db.Projects.AnyAsync(p => p.Id == projectId))
            throw AppException.NotFound("Project");

        // type=vendor 時必須指定有效（且 IsActive）廠商；其他類型強制清空 vendorId
        if (type == "vendor")
        {
            if (!vendorId.HasValue)
                return new BadRequestObjectResult(ApiResponse.Fail("廠商請款必須指定廠商。"));
            var vendor = await db.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vendorId.Value);
            if (vendor is null)
                return new NotFoundObjectResult(ApiResponse.Fail("指定的廠商不存在。"));
            if (!vendor.IsActive)
                return new BadRequestObjectResult(ApiResponse.Fail("此廠商已停用，無法選用。"));
        }
        else
        {
            vendorId = null;
        }

        // 指定審核者存在性驗證
        if (designatedReviewers is { Length: > 0 })
        {
            var reviewerIds = designatedReviewers.Select(r => r.ReviewerId).Distinct().ToList();
            var existCount = await db.Users.AsNoTracking().CountAsync(u => reviewerIds.Contains(u.Id));
            if (existCount != reviewerIds.Count)
                return new BadRequestObjectResult(ApiResponse.Fail("一或多位指定審核者不存在。"));
        }

        var invoices = JsonSerializer.Deserialize<InvoiceMetadata[]>(invoicesJson, JsonOpts);
        if (invoices is null || invoices.Length == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("At least one invoice is required."));

        // 產生請款單號：PR-yyyyMMdd-NNN（唯一索引保護並發）
        var today = Clock.Now;
        var prefix = $"PR-{today:yyyyMMdd}-";
        var maxNo = await db.PaymentRequests
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

        // 發票號碼含中文 / CJK 者（如「收據」「領據」）視為手打文字，排除於重複檢查之外
        var checkableInvoices = invoices
            .Where(i => !InvoiceNoHelper.IsManualText(i.InvoiceNo))
            .ToList();

        // 批次內重複檢查
        var duplicatesInBatch = checkableInvoices
            .GroupBy(i => i.InvoiceNo)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicatesInBatch.Count > 0)
            throw AppException.Conflict($"發票號碼重複：{string.Join(", ", duplicatesInBatch)}");

        // 資料庫唯一性檢查（排除已拒絕的請款單，並跨沖銷表檢查）
        var invoiceNos = checkableInvoices.Select(i => i.InvoiceNo).ToList();
        if (invoiceNos.Count > 0)
        {
            var existInInvoice = await db.InvoiceItems
                .Where(ii => invoiceNos.Contains(ii.InvoiceNo)
                          && ii.PaymentRequest.ApprovalStatus != "rejected")
                .Select(ii => ii.InvoiceNo)
                .Distinct()
                .ToListAsync();
            var existInWriteOff = await db.Set<WriteOffItem>()
                .Where(wi => invoiceNos.Contains(wi.InvoiceNo!))
                .Select(wi => wi.InvoiceNo!)
                .Distinct()
                .ToListAsync();
            var existingNos = existInInvoice.Union(existInWriteOff).Distinct().ToList();
            if (existingNos.Count > 0)
                throw AppException.Conflict($"發票號碼已存在：{string.Join(", ", existingNos)}");
        }

        // 上傳檔案至 Blob Storage
        var files = form.Files.GetFiles("files");
        var invoiceItems = new List<InvoiceItem>();
        foreach (var inv in invoices)
        {
            string? fileUrl = null;
            if (inv.FileIndex >= 0 && inv.FileIndex < files.Count)
            {
                var file = files[inv.FileIndex];
                var ext = Path.GetExtension(file.FileName);
                var blobName = $"{Clock.Now:yyyy/MM}/{Guid.NewGuid()}{ext}";
                using var stream = file.OpenReadStream();
                fileUrl = await blob.UploadAsync(ContainerName, blobName, stream, file.ContentType);
            }

            invoiceItems.Add(new InvoiceItem
            {
                FileName    = inv.FileName,
                InvoiceNo   = inv.InvoiceNo,
                Amount      = inv.Amount,
                ItemName    = inv.ItemName,
                Note        = inv.Note,
                FileUrl     = fileUrl,
                InvoiceDate = inv.InvoiceDate,
            });
        }

        var pr = new PaymentRequest
        {
            RequestNo     = requestNo,
            Type          = type,
            ProjectId     = projectId,
            VendorId      = vendorId,
            Reason        = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            SubmittedById = submittedById,   // 強制使用 JWT 身分
            TotalAmount   = invoices.Sum(i => i.Amount),
            ApprovalStatus = "draft",
            CreatedAt     = today,
        };
        pr.InvoiceItems = invoiceItems;

        // 整單批次附件（照片 / PDF）— 一般請款才會帶入；其他類型前端送空陣列
        var attachmentsJson = form["attachments"].ToString();
        if (!string.IsNullOrEmpty(attachmentsJson))
        {
            var attMetas    = JsonSerializer.Deserialize<AttachmentProcessor.AttachmentMetadata[]>(attachmentsJson, JsonOpts) ?? [];
            var attFiles    = form.Files.GetFiles("attachmentFiles");
            var resolvedAtt = await AttachmentProcessor.ResolveAsync(attMetas, attFiles, blob);
            pr.Attachments  = resolvedAtt.Select((a, i) => new PaymentRequestAttachment
            {
                FileName  = a.FileName,
                FileUrl   = a.FileUrl,
                SortOrder = i,
            }).ToList();
        }

        db.PaymentRequests.Add(pr);
        await db.SaveChangesAsync();

        // 儲存指定審核者
        if (designatedReviewers is { Length: > 0 })
        {
            db.RequestDesignatedReviewers.AddRange(
                DesignatedReviewerHelper.BuildEntities("payment_request", pr.Id, designatedReviewers));
            await db.SaveChangesAsync();
        }

        var dto = await reader.GetByIdAsync(pr.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Payment request created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid payment request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var pr = currentUser?.IsSuperAdmin == true
            ? await db.PaymentRequests.Include(x => x.InvoiceItems).Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.PaymentRequests.Include(x => x.InvoiceItems).Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (pr is null) throw AppException.NotFound("PaymentRequest");

        if (pr.ApprovalStatus != "draft" && pr.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned payment requests can be edited.");

        var form = await req.ReadFormAsync();

        var type      = form["type"].ToString();
        var projectId = int.TryParse(form["projectId"], out var pid) ? pid : (int?)null;
        var reason    = form["reason"].ToString();
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

        // 更新請款原因（必填，不可清空）
        if (form.ContainsKey("reason"))
        {
            if (string.IsNullOrWhiteSpace(reason))
                return new BadRequestObjectResult(ApiResponse.Fail("請填寫請款原因。"));
            pr.Reason = reason.Trim();
        }

        if (projectId.HasValue)
        {
            if (!await db.Projects.AnyAsync(p => p.Id == projectId))
                throw AppException.NotFound("Project");
            pr.ProjectId = projectId.Value;
        }

        // 廠商驗證與寫入：只要表單帶了 vendorId 或 type 欄位，就重新依當前 type 計算
        if (form.ContainsKey("vendorId") || form.ContainsKey("type"))
        {
            var vendorIdStr = form["vendorId"].ToString();
            int? vendorId = int.TryParse(vendorIdStr, out var vid) ? vid : null;

            if (pr.Type == "vendor")
            {
                if (!vendorId.HasValue)
                    return new BadRequestObjectResult(ApiResponse.Fail("廠商請款必須指定廠商。"));
                var vendor = await db.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vendorId.Value);
                if (vendor is null)
                    return new NotFoundObjectResult(ApiResponse.Fail("指定的廠商不存在。"));
                if (!vendor.IsActive && pr.VendorId != vendorId)
                    return new BadRequestObjectResult(ApiResponse.Fail("此廠商已停用，無法選用。"));
                pr.VendorId = vendorId;
            }
            else
            {
                pr.VendorId = null;
            }
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
                .Where(r => r.RequestType == "payment_request" && r.RequestId == intId)
                .ToListAsync();
            db.RequestDesignatedReviewers.RemoveRange(old);
            if (updateDesignatedReviewers.Length > 0)
            {
                db.RequestDesignatedReviewers.AddRange(
                    DesignatedReviewerHelper.BuildEntities("payment_request", intId, updateDesignatedReviewers));
            }
        }

        // 整單批次附件整組替換（提供 attachments 欄位才更新；blob 待主交易存檔後再清理）
        var removedAttachmentBlobs = new List<string>();
        if (form.ContainsKey("attachments"))
        {
            var attMetas    = JsonSerializer.Deserialize<AttachmentProcessor.AttachmentMetadata[]>(form["attachments"].ToString(), JsonOpts) ?? [];
            var attFiles    = form.Files.GetFiles("attachmentFiles");
            var oldAttUrls  = pr.Attachments.Where(a => !string.IsNullOrEmpty(a.FileUrl)).Select(a => a.FileUrl!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var resolvedAtt = await AttachmentProcessor.ResolveAsync(attMetas, attFiles, blob);
            var newAttUrls  = resolvedAtt.Where(a => !string.IsNullOrEmpty(a.FileUrl)).Select(a => a.FileUrl!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            db.PaymentRequestAttachments.RemoveRange(pr.Attachments);
            pr.Attachments = resolvedAtt.Select((a, i) => new PaymentRequestAttachment
            {
                FileName  = a.FileName,
                FileUrl   = a.FileUrl,
                SortOrder = i,
            }).ToList();
            removedAttachmentBlobs = oldAttUrls.Except(newAttUrls).ToList();
        }

        var invoicesJson = form["invoices"].ToString();
        if (!string.IsNullOrEmpty(invoicesJson))
        {
            var invoices = JsonSerializer.Deserialize<InvoiceMetadata[]>(invoicesJson, JsonOpts);
            if (invoices is null || invoices.Length == 0)
                return new BadRequestObjectResult(ApiResponse.Fail("At least one invoice is required."));

            // 發票號碼含中文 / CJK 者（如「收據」「領據」）視為手打文字，排除於重複檢查之外
            var checkableInvoices = invoices
                .Where(i => !InvoiceNoHelper.IsManualText(i.InvoiceNo))
                .ToList();

            // 批次內重複檢查
            var duplicatesInBatch = checkableInvoices
                .GroupBy(i => i.InvoiceNo)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicatesInBatch.Count > 0)
                throw AppException.Conflict($"發票號碼重複：{string.Join(", ", duplicatesInBatch)}");

            // 資料庫唯一性檢查（排除自己目前的發票 + 已拒絕的請款單，並跨沖銷表檢查）
            var invoiceNos = checkableInvoices.Select(i => i.InvoiceNo).ToList();
            if (invoiceNos.Count > 0)
            {
                var existInInvoice = await db.InvoiceItems
                    .Where(ii => invoiceNos.Contains(ii.InvoiceNo)
                              && ii.PaymentRequestId != intId
                              && ii.PaymentRequest.ApprovalStatus != "rejected")
                    .Select(ii => ii.InvoiceNo)
                    .Distinct()
                    .ToListAsync();
                var existInWriteOff = await db.Set<WriteOffItem>()
                    .Where(wi => invoiceNos.Contains(wi.InvoiceNo!))
                    .Select(wi => wi.InvoiceNo!)
                    .Distinct()
                    .ToListAsync();
                var existingNos = existInInvoice.Union(existInWriteOff).Distinct().ToList();
                if (existingNos.Count > 0)
                    throw AppException.Conflict($"發票號碼已存在：{string.Join(", ", existingNos)}");
            }

            // 收集舊 FileUrl（稍後比對，刪除不再使用的 blob）
            var oldFileUrls = pr.InvoiceItems
                .Where(ii => !string.IsNullOrEmpty(ii.FileUrl))
                .Select(ii => ii.FileUrl!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 上傳新檔案、保留既有 URL
            var files = form.Files.GetFiles("files");
            var newFileUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var invoiceItems = new List<InvoiceItem>();

            foreach (var inv in invoices)
            {
                string? fileUrl = inv.FileUrl; // 保留既有 URL
                if (inv.FileIndex >= 0 && inv.FileIndex < files.Count)
                {
                    var file = files[inv.FileIndex];
                    var ext = Path.GetExtension(file.FileName);
                    var blobName = $"{Clock.Now:yyyy/MM}/{Guid.NewGuid()}{ext}";
                    using var stream = file.OpenReadStream();
                    fileUrl = await blob.UploadAsync(ContainerName, blobName, stream, file.ContentType);
                }
                if (!string.IsNullOrEmpty(fileUrl))
                    newFileUrls.Add(fileUrl);

                invoiceItems.Add(new InvoiceItem
                {
                    FileName    = inv.FileName,
                    InvoiceNo   = inv.InvoiceNo,
                    Amount      = inv.Amount,
                    ItemName    = inv.ItemName,
                    Note        = inv.Note,
                    FileUrl     = fileUrl,
                    InvoiceDate = inv.InvoiceDate,
                });
            }

            db.InvoiceItems.RemoveRange(pr.InvoiceItems);
            pr.InvoiceItems = invoiceItems;
            pr.TotalAmount  = invoices.Sum(i => i.Amount);

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

        var dto = await reader.GetByIdAsync(pr.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Payment request updated."));
    }

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid payment request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var pr = currentUser?.IsSuperAdmin == true
            ? await db.PaymentRequests.Include(x => x.InvoiceItems).Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.PaymentRequests.Include(x => x.InvoiceItems).Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (pr is null) throw AppException.NotFound("PaymentRequest");

        if (pr.ApprovalStatus != "draft" && pr.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned payment requests can be deleted.");

        // 收集要刪除的 blob（發票 + 整單附件）
        var blobNames = pr.InvoiceItems
            .Select(ii => blob.ExtractBlobName(ii.FileUrl, ContainerName))
            .Where(n => n is not null)
            .ToList();
        var attachmentBlobNames = pr.Attachments
            .Select(a => blob.ExtractBlobName(a.FileUrl, AttachmentProcessor.ContainerName))
            .Where(n => n is not null)
            .ToList();

        // 一併清除此申請單的審核流程足跡（多型關聯無 FK，須手動刪除，否則殘留列會擋住使用者刪除）
        db.ApprovalRecords.RemoveRange(
            await db.ApprovalRecords.Where(r => r.ApplicationType == "payment_request" && r.ApplicationId == pr.Id).ToListAsync());
        db.EscalationOverrides.RemoveRange(
            await db.EscalationOverrides.Where(o => o.ApplicationType == "payment_request" && o.ApplicationId == pr.Id).ToListAsync());
        db.RequestDesignatedReviewers.RemoveRange(
            await db.RequestDesignatedReviewers.Where(r => r.RequestType == "payment_request" && r.RequestId == pr.Id).ToListAsync());

        db.PaymentRequests.Remove(pr);
        await db.SaveChangesAsync();

        // 刪除 blob files
        foreach (var name in blobNames)
            await blob.DeleteAsync(ContainerName, name!);
        foreach (var name in attachmentBlobNames)
            await blob.DeleteAsync(AttachmentProcessor.ContainerName, name!);

        return new OkObjectResult(ApiResponse.Ok($"Payment request '{id}' deleted."));
    }

    /// <summary>送出申請（draft → pending）</summary>
    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid payment request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var pr = currentUser?.IsSuperAdmin == true
            ? await db.PaymentRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.PaymentRequests.FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (pr is null) throw AppException.NotFound("PaymentRequest");

        if (pr.ApprovalStatus != "draft" && pr.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned payment requests can be submitted.");

        // 退回重送時清除舊審核記錄，重置指定審核者狀態，重新走流程
        if (pr.ApprovalStatus == "returned")
        {
            var oldRecords = await db.ApprovalRecords
                .Where(r => r.ApplicationType == "payment_request" && r.ApplicationId == pr.Id)
                .ToListAsync();
            db.ApprovalRecords.RemoveRange(oldRecords);

            var oldOverrides = await db.EscalationOverrides
                .Where(o => o.ApplicationType == "payment_request" && o.ApplicationId == pr.Id)
                .ToListAsync();
            db.EscalationOverrides.RemoveRange(oldOverrides);

            // 重置指定審核者狀態為 pending（重送需重新走指定審核流程）
            var rdrsToReset = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == "payment_request" && r.RequestId == pr.Id)
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
        if (pr.ApprovalItemId is null)
            pr.ApprovalItemId = await approvalFlow.ResolveApprovalItemIdAsync("payment_request", submitter?.DepartmentId);

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
            return new OkObjectResult(ApiResponse.Ok(saDto, "Payment request auto-approved."));
        }

        // 若流程中有 UseApplicantDesignated 步驟，正規化各 designee 所屬步驟並驗證每步皆有指定審核者
        await DesignatedReviewerHelper.ValidateAndNormalizeAsync(db, "payment_request", pr.Id, pr.ApprovalItemId, userId);
        await db.SaveChangesAsync();

        // 查詢指定審核者清單傳給 ResolveStartingStepAsync（含 ApprovalStepOrder 綁定步驟）
        var designatedReviewers = await DesignatedReviewerHelper.ReadForFlowAsync(db, "payment_request", pr.Id);

        // 自動跳過「申請人即審核者」的步驟
        var (startStep, autoApproved, _) = await approvalFlow.ResolveStartingStepAsync(
            pr.ApprovalItemId, userId, "payment_request", designatedReviewers);

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

        // 通知審核者：若為指定審核步驟則通知第一位指定審核者，否則通知符合條件的審核者
        if (!autoApproved && pr.SubmittedById.HasValue)
        {
            // 指定審核步驟（原生 UseApplicantDesignated 或例外指定審核命中）：讀 designee 快照，
            // 與 ResolveStartingStepAsync 的判定同源，確保不會誤走部門/職稱通知
            bool isDesignatedStep = designatedReviewers.Any(r => r.ApprovalStepOrder == startStep);
            if (isDesignatedStep)
            {
                var firstReviewer = await db.RequestDesignatedReviewers
                    .AsNoTracking()
                    .Where(r => r.RequestType == "payment_request" && r.RequestId == pr.Id
                             && r.ApprovalStepOrder == startStep && r.Status == "pending")
                    .OrderBy(r => r.StepOrder)
                    .FirstOrDefaultAsync();
                if (firstReviewer is not null)
                    await notifier.NotifySpecificReviewerAsync("payment_request", pr.Id, firstReviewer.ReviewerId, pr.SubmittedById.Value, false);
            }
            else
                await notifier.NotifyReviewersAsync("payment_request", pr.Id, pr.ApprovalItemId, startStep, pr.SubmittedById.Value);
        }

        var dto = await reader.GetByIdAsync(pr.Id);
        var msg = autoApproved ? "Payment request auto-approved." : "Payment request submitted.";
        return new OkObjectResult(ApiResponse.Ok(dto, msg));
    }

    /// <summary>
    /// 新增 / 更新分期撥款明細（4 種申請類型共用語意）。
    /// 僅財務體系部門或 Superadmin 可操作。
    /// 每筆新填入 PaidAt 的 installment 觸發一次「已撥款」通知（含 N/M 期）。
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

        var pr = await db.PaymentRequests
                         .Include(p => p.Installments)
                         .FirstOrDefaultAsync(p => p.Id == intId)
                 ?? throw AppException.NotFound("PaymentRequest");

        if (pr.ApprovalStatus != "approved")
            return new BadRequestObjectResult(ApiResponse.Fail("只有已核准的請款申請可以設定撥款明細。"));

        var body = await req.ReadFromJsonAsync<UpsertInstallmentsRequest>(JsonOpts);
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        // 共用 validate + diff（不 SaveChanges）
        var newlyPaid = InstallmentUpsertService.Apply(
            db, pr.Installments, body.Installments, pr.TotalAmount, userId,
            () => new PaymentRequestInstallment { PaymentRequestId = pr.Id });

        // 3. 狀態（可選）
        if (!string.IsNullOrWhiteSpace(body.ApprovalStatus))
        {
            var allowed = new[] { "draft", "pending", "approved", "returned", "rejected" };
            if (!allowed.Contains(body.ApprovalStatus))
                return new BadRequestObjectResult(ApiResponse.Fail($"不合法的狀態值：{body.ApprovalStatus}"));
            pr.ApprovalStatus = body.ApprovalStatus;
        }

        await db.SaveChangesAsync();

        // 4. 逐筆通知
        if (pr.SubmittedById.HasValue)
            foreach (var np in newlyPaid)
                await notifier.NotifyApplicantPaidAsync(
                    "payment_request", pr.Id, pr.SubmittedById.Value, np.Amount, np.PaidAt,
                    installmentNo: np.InstallmentNo, totalInstallments: np.TotalInstallments);

        return new OkObjectResult(ApiResponse.Ok(
            new { pr.Id, pr.ApprovalStatus, InstallmentCount = body.Installments.Count },
            $"已更新 {body.Installments.Count} 筆撥款明細。"));
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
