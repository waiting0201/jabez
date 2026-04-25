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

public sealed class AdvanceRequestHandler(
    AppDbContext db,
    IAdvanceRequestReadService reader,
    IBlobStorageService blob,
    IJwtService jwtService,
    IApprovalNotificationService notifier,
    IApprovalFlowService approvalFlow)
{
    private const string ContainerName = "advance-files";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Multipart form 中 items JSON 的內部結構</summary>
    private sealed record ItemMetadata(
        string   Category,
        int      SeqNo,
        string   ItemName,
        decimal  UnitPrice,
        string   Quantity,
        decimal  TotalPrice,
        decimal  CashAmount,
        decimal  CheckAmount,
        string?  Note,
        int      SortOrder,
        string?  FileName,
        string?  FileUrl,
        int      FileIndex);
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
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advance request ID format."));

        var item = await reader.GetByIdAsync(intId);
        if (item is null)
            return new NotFoundObjectResult(ApiResponse.Fail("Advance request not found."));

        return new OkObjectResult(ApiResponse.Ok(item));
    }

    // ── 新增（草稿）─────────────────────────────────────────────────────────

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var submittedById = await GetUserIdAsync(req);

        var form = await req.ReadFormAsync();

        var projectId      = int.TryParse(form["projectId"], out var pid) ? pid : 0;
        var activityName   = form["activityName"].ToString();
        var activityPeriod = form["activityPeriod"].ToString();
        var advanceDateStr = form["advanceDate"].ToString();
        var itemsJson      = form["items"].ToString();
        var drJson         = form["designatedReviewers"].ToString();

        if (!DateTime.TryParse(advanceDateStr, out var advanceDate))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advanceDate."));

        var itemsMeta = JsonSerializer.Deserialize<ItemMetadata[]>(itemsJson, JsonOpts);
        if (itemsMeta is null || itemsMeta.Length == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("At least one item is required."));

        if (!await db.Projects.AnyAsync(p => p.Id == projectId))
            throw AppException.NotFound("Project");

        // 指定審核者
        DesignatedReviewerRequest[]? designatedReviewers = null;
        if (!string.IsNullOrEmpty(drJson))
            designatedReviewers = JsonSerializer.Deserialize<DesignatedReviewerRequest[]>(drJson, JsonOpts);

        if (designatedReviewers is { Length: > 0 })
        {
            var reviewerIds = designatedReviewers.Select(r => r.ReviewerId).Distinct().ToList();
            var existCount = await db.Users.AsNoTracking().CountAsync(u => reviewerIds.Contains(u.Id));
            if (existCount != reviewerIds.Count)
                return new BadRequestObjectResult(ApiResponse.Fail("一或多位指定審核者不存在。"));
        }

        // 產生預支單號：ADV-yyyyMMdd-NNN（唯一索引保護並發）
        var today = Clock.Now;
        var prefix = $"ADV-{today:yyyyMMdd}-";
        var maxNo = await db.AdvanceRequests
            .Where(a => a.RequestNo.StartsWith(prefix))
            .MaxAsync(a => (string?)a.RequestNo);
        int seq = 1;
        if (maxNo is not null)
        {
            var seqStr = maxNo[prefix.Length..];
            if (int.TryParse(seqStr, out var parsed))
                seq = parsed + 1;
        }
        var requestNo = $"{prefix}{seq:D3}";

        // 上傳檔案至 Blob Storage
        var files = form.Files.GetFiles("files");
        var items = new List<AdvanceRequestItem>();
        foreach (var (meta, idx) in itemsMeta.Select((m, i) => (m, i)))
        {
            string? fileUrl  = null;
            string? fileName = meta.FileName;
            if (meta.FileIndex >= 0 && meta.FileIndex < files.Count)
            {
                var file = files[meta.FileIndex];
                var ext = Path.GetExtension(file.FileName);
                var blobName = $"{Clock.Now:yyyy/MM}/{Guid.NewGuid()}{ext}";
                using var stream = file.OpenReadStream();
                fileUrl  = await blob.UploadAsync(ContainerName, blobName, stream, file.ContentType);
                fileName = file.FileName;
            }

            items.Add(new AdvanceRequestItem
            {
                Category    = meta.Category,
                SeqNo       = meta.SeqNo,
                ItemName    = meta.ItemName,
                UnitPrice   = meta.UnitPrice,
                Quantity    = meta.Quantity,
                TotalPrice  = meta.TotalPrice,
                CashAmount  = meta.CashAmount,
                CheckAmount = meta.CheckAmount,
                Note        = meta.Note,
                SortOrder   = meta.SortOrder > 0 ? meta.SortOrder : idx,
                FileName    = fileName,
                FileUrl     = fileUrl,
            });
        }

        var ar = new AdvanceRequest
        {
            RequestNo      = requestNo,
            ProjectId      = projectId,
            ActivityName   = activityName,
            ActivityPeriod = activityPeriod,
            AdvanceDate    = advanceDate,
            CashTotal      = items.Sum(i => i.CashAmount),
            CheckTotal     = items.Sum(i => i.CheckAmount),
            GrandTotal     = items.Sum(i => i.TotalPrice),
            SubmittedById  = submittedById,
            ApprovalStatus = "draft",
            CreatedAt      = today,
        };
        ar.Items = items;

        db.AdvanceRequests.Add(ar);
        await db.SaveChangesAsync();

        // 儲存指定審核者
        if (designatedReviewers is { Length: > 0 })
        {
            db.RequestDesignatedReviewers.AddRange(
                designatedReviewers.OrderBy(r => r.StepOrder).Select(r => new RequestDesignatedReviewer
                {
                    RequestType = "advance",
                    RequestId   = ar.Id,
                    ReviewerId  = r.ReviewerId,
                    StepOrder   = r.StepOrder,
                }));
            await db.SaveChangesAsync();
        }

        var dto = await reader.GetByIdAsync(ar.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Advance request created.")) { StatusCode = 201 };
    }

    // ── 更新草稿 ────────────────────────────────────────────────────────────

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advance request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var ar = currentUser?.IsSuperAdmin == true
            ? await db.AdvanceRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.AdvanceRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (ar is null) throw AppException.NotFound("AdvanceRequest");

        if (ar.ApprovalStatus != "draft" && ar.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned advance requests can be edited.");

        var form = await req.ReadFormAsync();

        var projectIdStr   = form["projectId"].ToString();
        var activityName   = form["activityName"].ToString();
        var activityPeriod = form["activityPeriod"].ToString();
        var advanceDateStr = form["advanceDate"].ToString();
        var itemsJson      = form["items"].ToString();
        var drJson         = form["designatedReviewers"].ToString();

        if (int.TryParse(projectIdStr, out var projectId) && projectId > 0)
        {
            if (!await db.Projects.AnyAsync(p => p.Id == projectId))
                throw AppException.NotFound("Project");
            ar.ProjectId = projectId;
        }
        if (!string.IsNullOrEmpty(activityName))
            ar.ActivityName = activityName;
        if (!string.IsNullOrEmpty(activityPeriod))
            ar.ActivityPeriod = activityPeriod;
        if (DateTime.TryParse(advanceDateStr, out var advanceDate))
            ar.AdvanceDate = advanceDate;

        // 指定審核者整組替換
        if (!string.IsNullOrEmpty(drJson) || form.ContainsKey("designatedReviewers"))
        {
            DesignatedReviewerRequest[]? designatedReviewers = null;
            if (!string.IsNullOrEmpty(drJson))
                designatedReviewers = JsonSerializer.Deserialize<DesignatedReviewerRequest[]>(drJson, JsonOpts);

            if (designatedReviewers is { Length: > 0 })
            {
                var reviewerIds = designatedReviewers.Select(r => r.ReviewerId).Distinct().ToList();
                var existCount = await db.Users.AsNoTracking().CountAsync(u => reviewerIds.Contains(u.Id));
                if (existCount != reviewerIds.Count)
                    return new BadRequestObjectResult(ApiResponse.Fail("一或多位指定審核者不存在。"));
            }
            var old = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == "advance" && r.RequestId == intId)
                .ToListAsync();
            db.RequestDesignatedReviewers.RemoveRange(old);
            if (designatedReviewers is { Length: > 0 })
            {
                db.RequestDesignatedReviewers.AddRange(
                    designatedReviewers.Select(r => new RequestDesignatedReviewer
                    {
                        RequestType = "advance",
                        RequestId   = intId,
                        ReviewerId  = r.ReviewerId,
                        StepOrder   = r.StepOrder,
                    }));
            }
        }

        // 更新明細（含檔案上傳）
        if (!string.IsNullOrEmpty(itemsJson))
        {
            var itemsMeta = JsonSerializer.Deserialize<ItemMetadata[]>(itemsJson, JsonOpts);
            if (itemsMeta is { Length: > 0 })
            {
                // 收集舊的 blob URLs
                var oldFileUrls = ar.Items
                    .Where(i => !string.IsNullOrEmpty(i.FileUrl))
                    .Select(i => i.FileUrl!)
                    .ToHashSet();

                db.AdvanceRequestItems.RemoveRange(ar.Items);

                var files = form.Files.GetFiles("files");
                var newItems = new List<AdvanceRequestItem>();
                var newFileUrls = new HashSet<string>();

                foreach (var (meta, idx) in itemsMeta.Select((m, i) => (m, i)))
                {
                    string? fileUrl  = meta.FileUrl;
                    string? fileName = meta.FileName;

                    if (meta.FileIndex >= 0 && meta.FileIndex < files.Count)
                    {
                        var file = files[meta.FileIndex];
                        var ext = Path.GetExtension(file.FileName);
                        var blobName = $"{Clock.Now:yyyy/MM}/{Guid.NewGuid()}{ext}";
                        using var stream = file.OpenReadStream();
                        fileUrl  = await blob.UploadAsync(ContainerName, blobName, stream, file.ContentType);
                        fileName = file.FileName;
                    }

                    if (!string.IsNullOrEmpty(fileUrl))
                        newFileUrls.Add(fileUrl);

                    newItems.Add(new AdvanceRequestItem
                    {
                        AdvanceRequestId = ar.Id,
                        Category    = meta.Category,
                        SeqNo       = meta.SeqNo,
                        ItemName    = meta.ItemName,
                        UnitPrice   = meta.UnitPrice,
                        Quantity    = meta.Quantity,
                        TotalPrice  = meta.TotalPrice,
                        CashAmount  = meta.CashAmount,
                        CheckAmount = meta.CheckAmount,
                        Note        = meta.Note,
                        SortOrder   = meta.SortOrder > 0 ? meta.SortOrder : idx,
                        FileName    = fileName,
                        FileUrl     = fileUrl,
                    });
                }

                ar.Items      = newItems;
                ar.CashTotal  = newItems.Sum(i => i.CashAmount);
                ar.CheckTotal = newItems.Sum(i => i.CheckAmount);
                ar.GrandTotal = newItems.Sum(i => i.TotalPrice);

                await db.SaveChangesAsync();

                // 刪除不再引用的舊 blob
                foreach (var oldUrl in oldFileUrls.Except(newFileUrls))
                {
                    var blobName = blob.ExtractBlobName(oldUrl, ContainerName);
                    if (blobName is not null)
                        await blob.DeleteAsync(ContainerName, blobName);
                }

                var dto = await reader.GetByIdAsync(ar.Id);
                return new OkObjectResult(ApiResponse.Ok(dto, "Advance request updated."));
            }
        }

        await db.SaveChangesAsync();

        var dtoResult = await reader.GetByIdAsync(ar.Id);
        return new OkObjectResult(ApiResponse.Ok(dtoResult, "Advance request updated."));
    }

    // ── 刪除草稿 ────────────────────────────────────────────────────────────

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advance request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var ar = currentUser?.IsSuperAdmin == true
            ? await db.AdvanceRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.AdvanceRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (ar is null) throw AppException.NotFound("AdvanceRequest");

        if (ar.ApprovalStatus != "draft" && ar.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned advance requests can be deleted.");

        // 收集需清理的 blob
        var blobNames = ar.Items
            .Select(i => blob.ExtractBlobName(i.FileUrl, ContainerName))
            .Where(n => n is not null)
            .ToList();

        db.AdvanceRequests.Remove(ar);
        await db.SaveChangesAsync();

        // 刪除 blob
        foreach (var name in blobNames)
            await blob.DeleteAsync(ContainerName, name!);

        return new OkObjectResult(ApiResponse.Ok($"Advance request '{id}' deleted."));
    }

    // ── 送出申請 ────────────────────────────────────────────────────────────

    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advance request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var ar = currentUser?.IsSuperAdmin == true
            ? await db.AdvanceRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.AdvanceRequests.FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        if (ar is null) throw AppException.NotFound("AdvanceRequest");

        if (ar.ApprovalStatus != "draft" && ar.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned advance requests can be submitted.");

        // 退回重送：清除舊審核記錄，重置指定審核者狀態
        if (ar.ApprovalStatus == "returned")
        {
            var oldRecords = await db.ApprovalRecords
                .Where(r => r.ApplicationType == "advance" && r.ApplicationId == ar.Id)
                .ToListAsync();
            db.ApprovalRecords.RemoveRange(oldRecords);

            var oldOverrides = await db.EscalationOverrides
                .Where(o => o.ApplicationType == "advance" && o.ApplicationId == ar.Id)
                .ToListAsync();
            db.EscalationOverrides.RemoveRange(oldOverrides);

            // 重置指定審核者狀態為 pending
            var rdrsToReset = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == "advance" && r.RequestId == ar.Id)
                .ToListAsync();
            foreach (var rdr in rdrsToReset)
            {
                rdr.Status     = "pending";
                rdr.ReviewedAt = null;
                rdr.Comment    = null;
            }
        }

        // 自動關聯簽核流程
        if (ar.ApprovalItemId is null)
        {
            var flow = await db.ApprovalItems
                .AsNoTracking()
                .FirstOrDefaultAsync(ai => ai.ApplicationType == "advance" && ai.IsActive);
            if (flow is not null)
                ar.ApprovalItemId = flow.Id;
        }

        // Superadmin 直接自動核准
        var submitter = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (submitter?.IsSuperAdmin == true)
        {
            ar.ApprovalStatus   = "approved";
            ar.CurrentStepOrder = 1;
            ar.ReviewedAt       = Clock.Now;
            ar.ReviewedById     = userId;
            ar.ReviewNote       = "系統自動核准（Superadmin）";
            await db.SaveChangesAsync();
            var saDto = await reader.GetByIdAsync(ar.Id);
            return new OkObjectResult(ApiResponse.Ok(saDto, "Advance request auto-approved."));
        }

        // 若流程中有 UseApplicantDesignated 步驟，必須有指定審核者
        if (ar.ApprovalItemId.HasValue)
        {
            bool hasDesignatedStep = await db.ApprovalSteps.AsNoTracking()
                .AnyAsync(s => s.ApprovalItemId == ar.ApprovalItemId && s.UseApplicantDesignated);
            if (hasDesignatedStep)
            {
                bool hasReviewers = await db.RequestDesignatedReviewers
                    .AnyAsync(r => r.RequestType == "advance" && r.RequestId == ar.Id);
                if (!hasReviewers)
                    return new BadRequestObjectResult(ApiResponse.Fail("此簽核流程包含申請人指定審核步驟，請提供指定審核者。"));
            }
        }

        // 查詢指定審核者清單傳給 ResolveStartingStepAsync
        var designatedReviewers = await db.RequestDesignatedReviewers
            .AsNoTracking()
            .Where(r => r.RequestType == "advance" && r.RequestId == ar.Id)
            .OrderBy(r => r.StepOrder)
            .Select(r => new DesignatedReviewerRequest(r.ReviewerId, r.StepOrder))
            .ToListAsync();

        // 自審跳過邏輯（與請款一致，不升級）
        var (startStep, autoApproved, _) = await approvalFlow.ResolveStartingStepAsync(
            ar.ApprovalItemId, userId, "advance", designatedReviewers);

        if (autoApproved)
        {
            ar.ApprovalStatus   = "approved";
            ar.CurrentStepOrder = startStep;
            ar.ReviewedAt       = Clock.Now;
            ar.ReviewedById     = userId;
            ar.ReviewNote       = "系統自動核准（所有審核步驟皆為申請人本人）";
        }
        else
        {
            ar.ApprovalStatus   = "pending";
            ar.CurrentStepOrder = startStep;
        }

        await db.SaveChangesAsync();

        if (!autoApproved && ar.SubmittedById.HasValue)
        {
            bool isDesignatedStep = ar.ApprovalItemId.HasValue && await db.ApprovalSteps.AsNoTracking()
                .AnyAsync(s => s.ApprovalItemId == ar.ApprovalItemId
                    && s.StepOrder == startStep
                    && s.UseApplicantDesignated);
            if (isDesignatedStep)
            {
                var firstReviewer = await db.RequestDesignatedReviewers
                    .AsNoTracking()
                    .Where(r => r.RequestType == "advance" && r.RequestId == ar.Id && r.Status == "pending")
                    .OrderBy(r => r.StepOrder)
                    .FirstOrDefaultAsync();
                if (firstReviewer is not null)
                    await notifier.NotifySpecificReviewerAsync("advance", ar.Id, firstReviewer.ReviewerId, ar.SubmittedById.Value, false);
            }
            else
                await notifier.NotifyReviewersAsync("advance", ar.Id, ar.ApprovalItemId, startStep, ar.SubmittedById.Value);
        }

        var dto = await reader.GetByIdAsync(ar.Id);
        var msg = autoApproved ? "Advance request auto-approved." : "Advance request submitted.";
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

        var ar = await db.AdvanceRequests.FindAsync(intId)
            ?? throw AppException.NotFound("AdvanceRequest");

        if (ar.ApprovalStatus != "approved")
            return new BadRequestObjectResult(ApiResponse.Fail("只有已核准的預支申請可以設定撥款日。"));

        var body = await req.ReadFromJsonAsync<UpdatePaymentDateRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (body.EstimatedPaymentDate.HasValue)
            ar.EstimatedPaymentDate = body.EstimatedPaymentDate.Value;
        if (body.PaidAt.HasValue)
        {
            ar.PaidAt = body.PaidAt.Value;
            ar.PaidByUserId = userId;
        }
        if (body.EstimatedRefundDate.HasValue)
            ar.EstimatedRefundDate = body.EstimatedRefundDate.Value;
        if (body.RefundedAt.HasValue)
        {
            ar.RefundedAt = body.RefundedAt.Value;
            ar.RefundedByUserId = userId;
        }
        if (body.RefundedAmount.HasValue)
            ar.RefundedAmount = body.RefundedAmount.Value;

        await db.SaveChangesAsync();

        var msg = (body.EstimatedRefundDate.HasValue || body.RefundedAt.HasValue || body.RefundedAmount.HasValue) ? "退款資訊已更新。" : "撥款日期已更新。";
        return new OkObjectResult(ApiResponse.Ok(new { ar.Id, ar.EstimatedPaymentDate, ar.PaidAt, ar.EstimatedRefundDate, ar.RefundedAt, ar.RefundedAmount }, msg));
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private async Task<Guid> GetUserIdAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw AppException.Unauthorized("Invalid token claims.");
        return userId;
    }
}
