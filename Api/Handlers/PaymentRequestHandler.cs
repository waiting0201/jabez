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
    private static readonly HashSet<string> ValidTypes = ["vendor", "travel", "advance"];
    private const string ContainerName = "invoices";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Multipart form 中 invoices JSON 的內部結構</summary>
    private sealed record InvoiceMetadata(
        string  FileName,
        string  InvoiceNo,
        decimal Amount,
        string? ItemName,
        string? Note,
        string? FileUrl,
        int     FileIndex);

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
        var invoicesJson = form["invoices"].ToString();
        var designatedReviewersJson = form["designatedReviewers"].ToString();
        DesignatedReviewerRequest[]? designatedReviewers = null;
        if (!string.IsNullOrEmpty(designatedReviewersJson))
            designatedReviewers = JsonSerializer.Deserialize<DesignatedReviewerRequest[]>(designatedReviewersJson, JsonOpts);

        if (string.IsNullOrEmpty(type) || !ValidTypes.Contains(type))
            return new BadRequestObjectResult(ApiResponse.Fail($"Invalid type '{type}'. Must be vendor, travel, or advance."));

        if (!await db.Projects.AnyAsync(p => p.Id == projectId))
            throw AppException.NotFound("Project");

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

        // 批次內重複檢查
        var duplicatesInBatch = invoices
            .GroupBy(i => i.InvoiceNo)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicatesInBatch.Count > 0)
            throw AppException.Conflict($"發票號碼重複：{string.Join(", ", duplicatesInBatch)}");

        // 資料庫唯一性檢查（排除已拒絕的請款單，並跨沖銷表檢查）
        var invoiceNos = invoices.Select(i => i.InvoiceNo).ToList();
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
                FileName  = inv.FileName,
                InvoiceNo = inv.InvoiceNo,
                Amount    = inv.Amount,
                ItemName  = inv.ItemName,
                Note      = inv.Note,
                FileUrl   = fileUrl,
            });
        }

        var pr = new PaymentRequest
        {
            Type          = type,
            ProjectId     = projectId,
            SubmittedById = submittedById,   // 強制使用 JWT 身分
            TotalAmount   = invoices.Sum(i => i.Amount),
            ApprovalStatus = "draft",
            CreatedAt     = Clock.Now,
        };
        pr.InvoiceItems = invoiceItems;

        db.PaymentRequests.Add(pr);
        await db.SaveChangesAsync();

        // 儲存指定審核者
        if (designatedReviewers is { Length: > 0 })
        {
            var rdrs = designatedReviewers
                .OrderBy(r => r.StepOrder)
                .Select(r => new RequestDesignatedReviewer
                {
                    RequestType = "payment_request",
                    RequestId   = pr.Id,
                    ReviewerId  = r.ReviewerId,
                    StepOrder   = r.StepOrder,
                }).ToList();
            db.RequestDesignatedReviewers.AddRange(rdrs);
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

        var pr = await db.PaymentRequests
                         .Include(x => x.InvoiceItems)
                         .FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId)
            ?? throw AppException.NotFound("PaymentRequest");

        if (pr.ApprovalStatus != "draft" && pr.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned payment requests can be edited.");

        var form = await req.ReadFormAsync();

        var type      = form["type"].ToString();
        var projectId = int.TryParse(form["projectId"], out var pid) ? pid : (int?)null;
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

        if (projectId.HasValue)
        {
            if (!await db.Projects.AnyAsync(p => p.Id == projectId))
                throw AppException.NotFound("Project");
            pr.ProjectId = projectId.Value;
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
                    updateDesignatedReviewers.Select(r => new RequestDesignatedReviewer
                    {
                        RequestType = "payment_request",
                        RequestId   = intId,
                        ReviewerId  = r.ReviewerId,
                        StepOrder   = r.StepOrder,
                    }));
            }
        }

        var invoicesJson = form["invoices"].ToString();
        if (!string.IsNullOrEmpty(invoicesJson))
        {
            var invoices = JsonSerializer.Deserialize<InvoiceMetadata[]>(invoicesJson, JsonOpts);
            if (invoices is null || invoices.Length == 0)
                return new BadRequestObjectResult(ApiResponse.Fail("At least one invoice is required."));

            // 批次內重複檢查
            var duplicatesInBatch = invoices
                .GroupBy(i => i.InvoiceNo)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicatesInBatch.Count > 0)
                throw AppException.Conflict($"發票號碼重複：{string.Join(", ", duplicatesInBatch)}");

            // 資料庫唯一性檢查（排除自己目前的發票 + 已拒絕的請款單，並跨沖銷表檢查）
            var invoiceNos = invoices.Select(i => i.InvoiceNo).ToList();
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
                    FileName  = inv.FileName,
                    InvoiceNo = inv.InvoiceNo,
                    Amount    = inv.Amount,
                    ItemName  = inv.ItemName,
                    Note      = inv.Note,
                    FileUrl   = fileUrl,
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

        var dto = await reader.GetByIdAsync(pr.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Payment request updated."));
    }

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid payment request ID format."));

        var pr = await db.PaymentRequests
                         .Include(x => x.InvoiceItems)
                         .FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId)
            ?? throw AppException.NotFound("PaymentRequest");

        if (pr.ApprovalStatus != "draft")
            throw AppException.BadRequest("Only draft payment requests can be deleted.");

        // 收集要刪除的 blob
        var blobNames = pr.InvoiceItems
            .Select(ii => blob.ExtractBlobName(ii.FileUrl, ContainerName))
            .Where(n => n is not null)
            .ToList();

        db.PaymentRequests.Remove(pr);
        await db.SaveChangesAsync();

        // 刪除 blob files
        foreach (var name in blobNames)
            await blob.DeleteAsync(ContainerName, name!);

        return new OkObjectResult(ApiResponse.Ok($"Payment request '{id}' deleted."));
    }

    /// <summary>送出申請（draft → pending）</summary>
    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid payment request ID format."));

        var pr = await db.PaymentRequests.FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId)
            ?? throw AppException.NotFound("PaymentRequest");

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

        // 自動關聯簽核流程（依 ApplicationType 查找啟用的流程）
        if (pr.ApprovalItemId is null)
        {
            var flow = await db.ApprovalItems
                .AsNoTracking()
                .FirstOrDefaultAsync(ai => ai.ApplicationType == "payment_request" && ai.IsActive);
            if (flow is not null)
                pr.ApprovalItemId = flow.Id;
        }

        // Superadmin 無部門歸屬，直接自動核准
        var submitter = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
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

        // 若流程中有 UseApplicantDesignated 步驟，必須有指定審核者
        if (pr.ApprovalItemId.HasValue)
        {
            bool hasDesignatedStep = await db.ApprovalSteps.AsNoTracking()
                .AnyAsync(s => s.ApprovalItemId == pr.ApprovalItemId && s.UseApplicantDesignated);
            if (hasDesignatedStep)
            {
                bool hasReviewers = await db.RequestDesignatedReviewers
                    .AnyAsync(r => r.RequestType == "payment_request" && r.RequestId == pr.Id);
                if (!hasReviewers)
                    return new BadRequestObjectResult(ApiResponse.Fail("此簽核流程包含申請人指定審核步驟，請提供指定審核者。"));
            }
        }

        // 查詢指定審核者清單傳給 ResolveStartingStepAsync
        var designatedReviewers = await db.RequestDesignatedReviewers
            .AsNoTracking()
            .Where(r => r.RequestType == "payment_request" && r.RequestId == pr.Id)
            .OrderBy(r => r.StepOrder)
            .Select(r => new DesignatedReviewerRequest(r.ReviewerId, r.StepOrder))
            .ToListAsync();

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
            bool isDesignatedStep = pr.ApprovalItemId.HasValue && await db.ApprovalSteps.AsNoTracking()
                .AnyAsync(s => s.ApprovalItemId == pr.ApprovalItemId
                    && s.StepOrder == startStep
                    && s.UseApplicantDesignated);
            if (isDesignatedStep)
            {
                var firstReviewer = await db.RequestDesignatedReviewers
                    .AsNoTracking()
                    .Where(r => r.RequestType == "payment_request" && r.RequestId == pr.Id && r.Status == "pending")
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

    /// <summary>更新已核准請款的撥款日期（僅財務部或 Superadmin）</summary>
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

        // 查詢操作者，確認是否財務部成員或 Superadmin
        var user = await db.Users.AsNoTracking().Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return new UnauthorizedObjectResult(ApiResponse.Fail("User not found."));

        // 只有財務部(FIN)或 Superadmin 可更新撥款日
        if (!user.IsSuperAdmin && user.Department?.Code != "FIN")
            return new ForbidResult();

        var pr = await db.PaymentRequests.FindAsync(intId)
            ?? throw AppException.NotFound("PaymentRequest");

        // paidAt 已有值 → 鎖定，不可再修改
        if (pr.PaidAt.HasValue)
            return new BadRequestObjectResult(ApiResponse.Fail("此請款已撥款，無法再修改。"));

        var body = await req.ReadFromJsonAsync<UpdatePaymentDateRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (body.EstimatedPaymentDate.HasValue)
            pr.EstimatedPaymentDate = body.EstimatedPaymentDate.Value;
        if (body.PaidAt.HasValue)
        {
            var taipeiTz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
            var nowTaipei = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, taipeiTz);
            pr.PaidAt = body.PaidAt.Value.Date + nowTaipei.TimeOfDay;
            pr.PaidByUserId = userId;
        }

        // 更新狀態（僅允許合法狀態值）
        if (!string.IsNullOrWhiteSpace(body.ApprovalStatus))
        {
            var allowed = new[] { "draft", "pending", "approved", "returned", "rejected" };
            if (!allowed.Contains(body.ApprovalStatus))
                return new BadRequestObjectResult(ApiResponse.Fail($"不合法的狀態值：{body.ApprovalStatus}"));
            pr.ApprovalStatus = body.ApprovalStatus;
        }

        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok(new { pr.Id, pr.ApprovalStatus, pr.EstimatedPaymentDate, pr.PaidAt }, "已更新。"));
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
