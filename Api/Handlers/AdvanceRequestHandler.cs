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
    private const string ContainerName = "write-off-invoices";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>沖銷明細 multipart JSON 的內部結構</summary>
    private sealed record WriteOffItemMetadata(
        string   Category,
        int      SeqNo,
        string   ItemName,
        decimal  UnitPrice,
        string   Quantity,
        decimal  TotalPrice,
        decimal  CashAmount,
        decimal  CheckAmount,
        string?  Note,
        string?  InvoiceNo,
        string?  FileName,
        string?  FileUrl,
        int      FileIndex,
        int      SortOrder);
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
        var body = await req.ReadFromJsonAsync<CreateAdvanceRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (body.Items.Length == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("At least one item is required."));

        if (!await db.Projects.AnyAsync(p => p.Id == body.ProjectId))
            throw AppException.NotFound("Project");

        // 指定審核者存在性驗證
        if (body.DesignatedReviewerId.HasValue)
        {
            var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == body.DesignatedReviewerId.Value);
            if (!exists)
                return new BadRequestObjectResult(ApiResponse.Fail("指定的審核者不存在。"));
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

        var items = body.Items.Select((i, idx) => new AdvanceRequestItem
        {
            Category    = i.Category,
            SeqNo       = i.SeqNo,
            ItemName    = i.ItemName,
            UnitPrice   = i.UnitPrice,
            Quantity    = i.Quantity,
            TotalPrice  = i.TotalPrice,
            CashAmount  = i.CashAmount,
            CheckAmount = i.CheckAmount,
            Note        = i.Note,
            SortOrder   = i.SortOrder > 0 ? i.SortOrder : idx,
        }).ToList();

        var ar = new AdvanceRequest
        {
            RequestNo            = requestNo,
            ProjectId            = body.ProjectId,
            ActivityName         = body.ActivityName,
            ActivityPeriod       = body.ActivityPeriod,
            AdvanceDate          = body.AdvanceDate,
            CashTotal            = items.Sum(i => i.CashAmount),
            CheckTotal           = items.Sum(i => i.CheckAmount),
            GrandTotal           = items.Sum(i => i.TotalPrice),
            SubmittedById        = submittedById,
            ApprovalStatus       = "draft",
            DesignatedReviewerId = body.DesignatedReviewerId,
            CreatedAt            = today,
        };
        ar.Items = items;

        db.AdvanceRequests.Add(ar);
        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(ar.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Advance request created.")) { StatusCode = 201 };
    }

    // ── 更新草稿 ────────────────────────────────────────────────────────────

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advance request ID format."));

        var ar = await db.AdvanceRequests
                         .Include(x => x.Items)
                         .FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId)
            ?? throw AppException.NotFound("AdvanceRequest");

        if (ar.ApprovalStatus != "draft" && ar.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned advance requests can be edited.");

        var body = await req.ReadFromJsonAsync<UpdateAdvanceRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (body.ProjectId.HasValue)
        {
            if (!await db.Projects.AnyAsync(p => p.Id == body.ProjectId))
                throw AppException.NotFound("Project");
            ar.ProjectId = body.ProjectId.Value;
        }
        if (!string.IsNullOrEmpty(body.ActivityName))
            ar.ActivityName = body.ActivityName;
        if (!string.IsNullOrEmpty(body.ActivityPeriod))
            ar.ActivityPeriod = body.ActivityPeriod;
        if (body.AdvanceDate.HasValue)
            ar.AdvanceDate = body.AdvanceDate.Value;
        // 指定審核者存在性驗證（提供非空 Guid 時才驗證）
        if (body.DesignatedReviewerId.HasValue && body.DesignatedReviewerId != Guid.Empty)
        {
            var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == body.DesignatedReviewerId.Value);
            if (!exists)
                return new BadRequestObjectResult(ApiResponse.Fail("指定的審核者不存在。"));
        }

        if (body.DesignatedReviewerId.HasValue)
            ar.DesignatedReviewerId = body.DesignatedReviewerId == Guid.Empty ? null : body.DesignatedReviewerId;

        if (body.Items is { Length: > 0 })
        {
            db.AdvanceRequestItems.RemoveRange(ar.Items);
            var newItems = body.Items.Select((i, idx) => new AdvanceRequestItem
            {
                AdvanceRequestId = ar.Id,
                Category    = i.Category,
                SeqNo       = i.SeqNo,
                ItemName    = i.ItemName,
                UnitPrice   = i.UnitPrice,
                Quantity    = i.Quantity,
                TotalPrice  = i.TotalPrice,
                CashAmount  = i.CashAmount,
                CheckAmount = i.CheckAmount,
                Note        = i.Note,
                SortOrder   = i.SortOrder > 0 ? i.SortOrder : idx,
            }).ToList();
            ar.Items      = newItems;
            ar.CashTotal  = newItems.Sum(i => i.CashAmount);
            ar.CheckTotal = newItems.Sum(i => i.CheckAmount);
            ar.GrandTotal = newItems.Sum(i => i.TotalPrice);
        }

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(ar.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Advance request updated."));
    }

    // ── 刪除草稿 ────────────────────────────────────────────────────────────

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advance request ID format."));

        var ar = await db.AdvanceRequests.FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId)
            ?? throw AppException.NotFound("AdvanceRequest");

        if (ar.ApprovalStatus != "draft")
            throw AppException.BadRequest("Only draft advance requests can be deleted.");

        db.AdvanceRequests.Remove(ar);
        await db.SaveChangesAsync();
        return new OkObjectResult(ApiResponse.Ok($"Advance request '{id}' deleted."));
    }

    // ── 送出申請 ────────────────────────────────────────────────────────────

    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advance request ID format."));

        var ar = await db.AdvanceRequests.FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId)
            ?? throw AppException.NotFound("AdvanceRequest");

        if (ar.ApprovalStatus != "draft" && ar.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned advance requests can be submitted.");

        // 退回重送：清除舊審核記錄
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

        // 若流程中有 UseApplicantDesignated 步驟，DesignatedReviewerId 必填
        if (ar.ApprovalItemId.HasValue)
        {
            bool hasDesignatedStep = await db.ApprovalSteps.AsNoTracking()
                .AnyAsync(s => s.ApprovalItemId == ar.ApprovalItemId && s.UseApplicantDesignated);
            if (hasDesignatedStep && !ar.DesignatedReviewerId.HasValue)
                return new BadRequestObjectResult(ApiResponse.Fail("此簽核流程包含申請人指定審核步驟，請提供 DesignatedReviewerId。"));
        }

        // 自審跳過邏輯（與請款一致，不升級）
        var (startStep, autoApproved, _) = await approvalFlow.ResolveStartingStepAsync(ar.ApprovalItemId, userId, "advance", ar.DesignatedReviewerId);

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
            if (isDesignatedStep && ar.DesignatedReviewerId.HasValue)
                await notifier.NotifySpecificReviewerAsync("advance", ar.Id, ar.DesignatedReviewerId.Value, ar.SubmittedById.Value, false);
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

        if (!user.IsSuperAdmin && user.Department?.Name != "財務部")
            return new ForbidResult();

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
            ar.PaidAt = body.PaidAt.Value;

        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok(new { ar.Id, ar.EstimatedPaymentDate, ar.PaidAt }, "撥款日期已更新。"));
    }

    // ── 沖銷：列表 ──────────────────────────────────────────────────────────

    public async Task<IActionResult> GetWriteOffsAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advance request ID format."));

        if (!await db.AdvanceRequests.AnyAsync(x => x.Id == intId))
            return new NotFoundObjectResult(ApiResponse.Fail("Advance request not found."));

        var records = await reader.GetWriteOffsAsync(intId);
        return new OkObjectResult(ApiResponse.Ok(records));
    }

    // ── 沖銷：新增（multipart/form-data，支援發票檔案上傳）─────────────────

    public async Task<IActionResult> CreateWriteOffAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advance request ID format."));

        var ar = await db.AdvanceRequests.FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId)
            ?? throw AppException.NotFound("AdvanceRequest");

        if (ar.ApprovalStatus != "approved")
            throw AppException.BadRequest("Only approved advance requests can have write-offs.");

        if (!ar.PaidAt.HasValue)
            throw AppException.BadRequest("預支尚未撥款，無法沖銷。");

        var form = await req.ReadFormAsync();
        var note = form["note"].ToString();
        var itemsJson = form["items"].ToString();

        var items = JsonSerializer.Deserialize<WriteOffItemMetadata[]>(itemsJson, JsonOpts);
        if (items is null || items.Length == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("At least one write-off item is required."));

        var cashTotal  = items.Sum(i => i.CashAmount);
        var checkTotal = items.Sum(i => i.CheckAmount);
        var grandTotal = items.Sum(i => i.TotalPrice);

        // 批次內發票號碼重複檢查
        var invoiceNos = items
            .Where(i => !string.IsNullOrWhiteSpace(i.InvoiceNo))
            .Select(i => i.InvoiceNo!)
            .ToList();
        if (invoiceNos.Count > 0)
        {
            var duplicatesInBatch = invoiceNos
                .GroupBy(n => n)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicatesInBatch.Count > 0)
                throw AppException.Conflict($"發票號碼重複：{string.Join(", ", duplicatesInBatch)}");

            // 資料庫唯一性檢查（跨所有沖銷 + 請款發票，排除已拒絕的申請）
            var existInWriteOff = await db.Set<WriteOffItem>()
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
            var existingNos = existInWriteOff.Union(existInInvoice).Distinct().ToList();
            if (existingNos.Count > 0)
                throw AppException.Conflict($"發票號碼已存在：{string.Join(", ", existingNos)}");
        }

        // 檢查累計沖銷金額不超過預支金額
        var existingTotal = await db.WriteOffRecords
            .Where(w => w.AdvanceRequestId == intId)
            .SumAsync(w => w.GrandTotal);
        if (existingTotal + grandTotal > ar.GrandTotal)
            throw AppException.BadRequest($"沖銷金額超過預支餘額。預支總額：{ar.GrandTotal:N0}，已沖銷：{existingTotal:N0}，本次沖銷：{grandTotal:N0}");

        // 取得下一個沖銷編號
        var lastNo = await db.WriteOffRecords
            .Where(w => w.AdvanceRequestId == intId)
            .OrderByDescending(w => w.WriteOffNo)
            .Select(w => w.WriteOffNo)
            .FirstOrDefaultAsync();

        // 上傳檔案至 Blob Storage
        var files = form.Files.GetFiles("files");
        var writeOffItems = new List<WriteOffItem>();
        foreach (var (item, idx) in items.Select((v, i) => (v, i)))
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

            writeOffItems.Add(new WriteOffItem
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
                SortOrder   = item.SortOrder > 0 ? item.SortOrder : idx,
            });
        }

        var wo = new WriteOffRecord
        {
            AdvanceRequestId = intId,
            WriteOffNo       = lastNo + 1,
            CashTotal        = cashTotal,
            CheckTotal       = checkTotal,
            GrandTotal       = grandTotal,
            Note             = note,
            SubmittedById    = userId,
            CreatedAt        = Clock.Now,
        };
        wo.Items = writeOffItems;

        db.WriteOffRecords.Add(wo);
        await db.SaveChangesAsync();

        var dto = await reader.GetWriteOffByIdAsync(wo.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Write-off created.")) { StatusCode = 201 };
    }

    // ── 沖銷：單筆查詢 ──────────────────────────────────────────────────────

    public async Task<IActionResult> GetWriteOffByIdAsync(HttpRequest req, string id, string writeOffId)
    {
        if (!int.TryParse(id, out var intId) || !int.TryParse(writeOffId, out var woId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid ID format."));

        if (!await db.AdvanceRequests.AnyAsync(x => x.Id == intId))
            return new NotFoundObjectResult(ApiResponse.Fail("Advance request not found."));

        var dto = await reader.GetWriteOffByIdAsync(woId);
        if (dto is null)
            return new NotFoundObjectResult(ApiResponse.Fail("Write-off not found."));
        return new OkObjectResult(ApiResponse.Ok(dto));
    }

    // ── 沖銷：刪除 ──────────────────────────────────────────────────────────

    public async Task<IActionResult> DeleteWriteOffAsync(HttpRequest req, string id, string writeOffId)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId) || !int.TryParse(writeOffId, out var woId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid ID format."));

        var wo = await db.WriteOffRecords
            .FirstOrDefaultAsync(w => w.Id == woId && w.AdvanceRequestId == intId && w.SubmittedById == userId)
            ?? throw AppException.NotFound("WriteOffRecord");

        db.WriteOffRecords.Remove(wo);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Write-off '{writeOffId}' deleted."));
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
