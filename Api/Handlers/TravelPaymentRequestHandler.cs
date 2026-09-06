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
/// 出差請款申請 Handler。
/// GET    /travel-payment-requests           → 列表（分頁）
/// POST   /travel-payment-requests           → 新增（multipart/form-data，含發票檔案）
/// GET    /travel-payment-requests/{id}      → 單筆
/// PUT    /travel-payment-requests/{id}      → 更新（multipart/form-data，僅 draft/returned 才允許）
/// PATCH  /travel-payment-requests/{id}      → 部分更新（同上）
/// DELETE /travel-payment-requests/{id}      → 刪除（僅 draft 才允許）
/// PATCH  /travel-payment-requests/{id}/submit       → 送出（draft → pending）
/// </summary>
public sealed class TravelPaymentRequestHandler(
    AppDbContext db,
    ITravelPaymentRequestReadService reader,
    IBlobStorageService blob,
    IJwtService jwtService,
    IApprovalNotificationService notifier,
    IApprovalFlowService approvalFlow)
{
    private const string AppType = "travel_payment";
    private const string ContainerName = "invoices";

    /// <summary>發票檔案允許的格式（與前端拖放 / OCR 支援一致）。</summary>
    private static readonly HashSet<string> AllowedInvoiceTypes =
        ["image/jpeg", "image/png", "image/gif", "image/webp", "image/heic", "application/pdf"];

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// 用 magic bytes 驗證上傳檔案的真實格式，不信任客戶端的 Content-Type。
    /// 回傳 (actualType, blobName)；若格式不在白名單則拋 BadRequest。
    /// </summary>
    private static async Task<(string ActualType, string BlobName)> ValidateAndBuildBlobNameAsync(IFormFile file)
    {
        string? actualType;
        using (var peek = file.OpenReadStream())
        {
            actualType = await FileSignatureValidator.DetectAsync(peek);
        }
        if (actualType is null || !AllowedInvoiceTypes.Contains(actualType))
            throw AppException.BadRequest("發票檔案僅支援 JPG / PNG / GIF / WebP / HEIC / PDF 格式。");

        var ext = Path.GetExtension(file.FileName);
        var blobName = $"{Clock.Now:yyyy/MM}/{Guid.NewGuid()}{ext}";
        return (actualType, blobName);
    }

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
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel payment request ID format."));

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var exists = user?.IsSuperAdmin == true
            ? await db.TravelPaymentRequests.AnyAsync(x => x.Id == intId)
            : await db.TravelPaymentRequests.AnyAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (!exists)
            return new NotFoundObjectResult(ApiResponse.Fail("Travel payment request not found."));

        var item = await reader.GetByIdAsync(intId);
        return new OkObjectResult(ApiResponse.Ok(item));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        // EmployeeId 由 JWT 中的 sub claim 決定，不信任客戶端傳入的值
        var employeeId = await GetUserIdAsync(req);

        var form = await req.ReadFormAsync();

        var destination = form["destination"].ToString();
        var purpose     = form["purpose"].ToString();
        var startDateStr = form["startDate"].ToString();
        var endDateStr   = form["endDate"].ToString();
        int? projectId   = int.TryParse(form["projectId"], out var pid) ? pid : null;
        int? approvalItemId = int.TryParse(form["approvalItemId"], out var aiid) ? aiid : null;

        if (string.IsNullOrWhiteSpace(destination))
            return new BadRequestObjectResult(ApiResponse.Fail("Destination is required."));
        if (!DateTime.TryParse(startDateStr, out var startDate) || !DateTime.TryParse(endDateStr, out var endDate))
            return new BadRequestObjectResult(ApiResponse.Fail("StartDate and EndDate are required."));
        if (endDate < startDate)
            return new BadRequestObjectResult(ApiResponse.Fail("EndDate must be on or after StartDate."));

        var itemsJson = form["items"].ToString();
        if (string.IsNullOrEmpty(itemsJson))
            return new BadRequestObjectResult(ApiResponse.Fail("At least one item is required."));
        var itemRequests = JsonSerializer.Deserialize<TravelPaymentRequestItemRequest[]>(itemsJson, JsonOpts);
        if (itemRequests is null || itemRequests.Length == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("At least one item is required."));

        // 年份合理性（擋民國年誤植）
        RequestDateGuard.EnsureAll((startDate, "出差開始日"), (endDate, "出差結束日"));
        RequestDateGuard.EnsureEach(itemRequests, i => i.InvoiceDate, "發票日期");

        var today = Clock.Now;

        // 指定審核者
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

        // 上傳檔案至 Blob Storage（依 FileIndex 對應）
        var files = form.Files.GetFiles("files");
        var entityItems = new List<TravelPaymentRequestItem>();
        for (int idx = 0; idx < itemRequests.Length; idx++)
        {
            var i = itemRequests[idx];
            string? fileUrl = null;
            string? fileName = i.FileName;
            if (i.FileIndex >= 0 && i.FileIndex < files.Count)
            {
                var file = files[i.FileIndex];
                var (actualType, blobName) = await ValidateAndBuildBlobNameAsync(file);
                using var stream = file.OpenReadStream();
                fileUrl  = await blob.UploadAsync(ContainerName, blobName, stream, actualType);
                fileName = file.FileName;
            }

            entityItems.Add(new TravelPaymentRequestItem
            {
                Category    = i.Category,
                SeqNo       = i.SeqNo,
                ItemName    = i.ItemName,
                UnitPrice   = i.UnitPrice,
                Quantity    = i.Quantity,
                TotalPrice  = i.TotalPrice,
                Note        = i.Note,
                SortOrder   = i.SortOrder > 0 ? i.SortOrder : idx,
                InvoiceNo   = string.IsNullOrWhiteSpace(i.InvoiceNo) ? null : i.InvoiceNo,
                InvoiceDate = i.InvoiceDate,
                FileName    = fileName,
                FileUrl     = fileUrl,
            });
        }

        var request = new TravelPaymentRequest
        {
            EmployeeId      = employeeId,
            ApprovalItemId  = approvalItemId,
            Destination     = destination,
            StartDate       = startDate,
            EndDate         = endDate,
            GrandTotal      = entityItems.Sum(i => i.TotalPrice),
            Purpose         = purpose,
            ProjectId       = projectId,
            ApprovalStatus  = "draft",
            CreatedAt       = today,
            Items           = entityItems,
        };
        db.TravelPaymentRequests.Add(request);
        await db.SaveChangesAsync();

        if (designatedReviewers is { Length: > 0 })
        {
            db.RequestDesignatedReviewers.AddRange(
                DesignatedReviewerHelper.BuildEntities(AppType, request.Id, designatedReviewers));
            await db.SaveChangesAsync();
        }

        var dto = await reader.GetByIdAsync(request.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Travel payment request created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel payment request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.TravelPaymentRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.TravelPaymentRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("TravelPaymentRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned travel payment requests can be edited.");

        var form = await req.ReadFormAsync();

        // 主檔欄位（皆為選填，未提供則保留原值）
        if (form.ContainsKey("destination"))
        {
            var destination = form["destination"].ToString();
            if (!string.IsNullOrWhiteSpace(destination)) item.Destination = destination;
        }
        if (form.ContainsKey("purpose"))
            item.Purpose = form["purpose"].ToString();
        // 年份合理性（擋民國年誤植，比照 CreateAsync）
        if (form.ContainsKey("startDate") && DateTime.TryParse(form["startDate"], out var startDate))
        {
            RequestDateGuard.Ensure(startDate, "出差開始日");
            item.StartDate = startDate;
        }
        if (form.ContainsKey("endDate") && DateTime.TryParse(form["endDate"], out var endDate))
        {
            RequestDateGuard.Ensure(endDate, "出差結束日");
            item.EndDate = endDate;
        }
        if (form.ContainsKey("projectId"))
        {
            if (int.TryParse(form["projectId"], out var pid))
                item.ProjectId = pid == 0 ? null : pid;
            else
                item.ProjectId = null;
        }

        // 指定審核者整組替換
        var drJson = form["designatedReviewers"].ToString();
        if (!string.IsNullOrEmpty(drJson))
        {
            var updateDesignatedReviewers = JsonSerializer.Deserialize<DesignatedReviewerRequest[]>(drJson, JsonOpts);
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
                    .Where(r => r.RequestType == AppType && r.RequestId == intId)
                    .ToListAsync();
                db.RequestDesignatedReviewers.RemoveRange(old);
                if (updateDesignatedReviewers.Length > 0)
                {
                    db.RequestDesignatedReviewers.AddRange(
                        DesignatedReviewerHelper.BuildEntities(AppType, intId, updateDesignatedReviewers));
                }
            }
        }

        // 明細整組替換（提供 items 時才更新）
        var itemsJson = form["items"].ToString();
        if (!string.IsNullOrEmpty(itemsJson))
        {
            var itemRequests = JsonSerializer.Deserialize<TravelPaymentRequestItemRequest[]>(itemsJson, JsonOpts);
            if (itemRequests is null || itemRequests.Length == 0)
                return new BadRequestObjectResult(ApiResponse.Fail("At least one item is required."));

            RequestDateGuard.EnsureEach(itemRequests, i => i.InvoiceDate, "發票日期");

            // 收集舊 FileUrl（稍後清理孤立 blob）
            var oldFileUrls = item.Items
                .Where(it => !string.IsNullOrEmpty(it.FileUrl))
                .Select(it => it.FileUrl!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var files = form.Files.GetFiles("files");
            var newFileUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var newItems = new List<TravelPaymentRequestItem>();

            for (int idx = 0; idx < itemRequests.Length; idx++)
            {
                var i = itemRequests[idx];
                string? fileUrl  = i.FileUrl;
                string? fileName = i.FileName;
                if (i.FileIndex >= 0 && i.FileIndex < files.Count)
                {
                    var file = files[i.FileIndex];
                    var (actualType, blobName) = await ValidateAndBuildBlobNameAsync(file);
                    using var stream = file.OpenReadStream();
                    fileUrl  = await blob.UploadAsync(ContainerName, blobName, stream, actualType);
                    fileName = file.FileName;
                }
                if (!string.IsNullOrEmpty(fileUrl))
                    newFileUrls.Add(fileUrl);

                newItems.Add(new TravelPaymentRequestItem
                {
                    TravelPaymentRequestId = intId,
                    Category    = i.Category,
                    SeqNo       = i.SeqNo,
                    ItemName    = i.ItemName,
                    UnitPrice   = i.UnitPrice,
                    Quantity    = i.Quantity,
                    TotalPrice  = i.TotalPrice,
                    Note        = i.Note,
                    SortOrder   = i.SortOrder > 0 ? i.SortOrder : idx,
                    InvoiceNo   = string.IsNullOrWhiteSpace(i.InvoiceNo) ? null : i.InvoiceNo,
                    InvoiceDate = i.InvoiceDate,
                    FileName    = fileName,
                    FileUrl     = fileUrl,
                });
            }

            db.TravelPaymentRequestItems.RemoveRange(item.Items);
            item.Items      = newItems;
            item.GrandTotal = newItems.Sum(it => it.TotalPrice);

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

        var dto = await reader.GetByIdAsync(item.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Travel payment request updated."));
    }

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel payment request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.TravelPaymentRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.TravelPaymentRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("TravelPaymentRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned travel payment requests can be deleted.");

        // 收集要刪除的 blob
        var blobNames = item.Items
            .Select(it => blob.ExtractBlobName(it.FileUrl, ContainerName))
            .Where(n => n is not null)
            .ToList();

        // 一併清除此申請單的審核流程足跡（多型關聯無 FK，須手動刪除，否則殘留列會擋住使用者刪除）
        db.ApprovalRecords.RemoveRange(
            await db.ApprovalRecords.Where(r => r.ApplicationType == AppType && r.ApplicationId == item.Id).ToListAsync());
        db.EscalationOverrides.RemoveRange(
            await db.EscalationOverrides.Where(o => o.ApplicationType == AppType && o.ApplicationId == item.Id).ToListAsync());
        db.RequestDesignatedReviewers.RemoveRange(
            await db.RequestDesignatedReviewers.Where(r => r.RequestType == AppType && r.RequestId == item.Id).ToListAsync());

        db.TravelPaymentRequests.Remove(item);
        await db.SaveChangesAsync();

        foreach (var name in blobNames)
            await blob.DeleteAsync(ContainerName, name!);

        return new OkObjectResult(ApiResponse.Ok($"Travel payment request '{id}' deleted."));
    }

    /// <summary>送出申請（draft → pending）</summary>
    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid travel payment request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.TravelPaymentRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.TravelPaymentRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("TravelPaymentRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned travel payment requests can be submitted.");

        // 送簽時才取號：單號日期＝送簽日，草稿不佔號。
        // 退回（returned）重送時已有單號，不可重新配號，否則已流通的單號會被改掉。
        if (string.IsNullOrEmpty(item.RequestNo))
            item.RequestNo = await RequestNoGenerator.NextAsync(
                db.TravelPaymentRequests.Select(x => x.RequestNo), "TPR-", Clock.Now);

        // 送簽日期只在首次送簽寫入：退回（returned）重送不改，與單號規則一致。
        item.SubmittedAt ??= Clock.Now;

        var hasItems = await db.TravelPaymentRequestItems.AnyAsync(i => i.TravelPaymentRequestId == intId);
        if (!hasItems)
            return new BadRequestObjectResult(ApiResponse.Fail("出差請款申請至少需要一筆費用明細項目。"));

        // 退回重送時清除舊審核記錄，重置指定審核者狀態，重新走流程
        if (item.ApprovalStatus == "returned")
        {
            var oldRecords = await db.ApprovalRecords
                .Where(r => r.ApplicationType == AppType && r.ApplicationId == item.Id)
                .ToListAsync();
            db.ApprovalRecords.RemoveRange(oldRecords);

            var oldOverrides = await db.EscalationOverrides
                .Where(o => o.ApplicationType == AppType && o.ApplicationId == item.Id)
                .ToListAsync();
            db.EscalationOverrides.RemoveRange(oldOverrides);

            // 重置指定審核者狀態為 pending
            var rdrsToReset = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == AppType && r.RequestId == item.Id)
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
            return new OkObjectResult(ApiResponse.Ok(saDto, "Travel payment request auto-approved."));
        }

        // 自動關聯簽核流程（依申請人部門挑流程：部門專屬優先，否則退回通用預設）
        if (item.ApprovalItemId is null)
            item.ApprovalItemId = await approvalFlow.ResolveApprovalItemIdAsync(AppType, submitter?.DepartmentId);

        // 正規化各 designee 所屬步驟並驗證每個指定審核步驟皆有審核者
        await DesignatedReviewerHelper.ValidateAndNormalizeAsync(db, AppType, item.Id, item.ApprovalItemId, userId);
        await db.SaveChangesAsync();

        // 查詢指定審核者清單傳給 ResolveStartingStepAsync（含 ApprovalStepOrder 綁定步驟）
        var designatedReviewers = await DesignatedReviewerHelper.ReadForFlowAsync(db, AppType, item.Id);

        var (startStep, autoApproved, escalation) =
            await approvalFlow.ResolveStartingStepAsync(item.ApprovalItemId, userId, AppType, designatedReviewers);

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

        if (escalation is not null)
        {
            db.EscalationOverrides.Add(new EscalationOverride
            {
                ApplicationType  = AppType,
                ApplicationId    = item.Id,
                StepOrder        = startStep,
                ReviewerId       = escalation.ReviewerId,
                OnBehalfOfUserId = escalation.OnBehalfOfUserId,
                CreatedAt        = Clock.Now,
            });
        }

        await db.SaveChangesAsync();

        if (!autoApproved)
        {
            if (escalation is not null)
                await notifier.NotifySpecificReviewerAsync(AppType, item.Id, escalation.ReviewerId, userId, escalation.OnBehalfOfUserId is not null);
            else
            {
                // 指定審核步驟（原生 UseApplicantDesignated 或例外指定審核命中）：讀 designee 快照，
                // 與 ResolveStartingStepAsync 的判定同源，確保不會誤走部門/職稱通知
                bool isDesignatedStep = designatedReviewers.Any(r => r.ApprovalStepOrder == startStep);
                if (isDesignatedStep)
                {
                    var firstReviewer = await db.RequestDesignatedReviewers
                        .AsNoTracking()
                        .Where(r => r.RequestType == AppType && r.RequestId == item.Id
                                 && r.ApprovalStepOrder == startStep && r.Status == "pending")
                        .OrderBy(r => r.StepOrder)
                        .FirstOrDefaultAsync();
                    if (firstReviewer is not null)
                        await notifier.NotifySpecificReviewerAsync(AppType, item.Id, firstReviewer.ReviewerId, userId, false);
                }
                else
                    await notifier.NotifyReviewersAsync(AppType, item.Id, item.ApprovalItemId, startStep, userId);
            }
        }

        var dto = await reader.GetByIdAsync(item.Id);
        var msg = autoApproved ? "Travel payment request auto-approved." : "Travel payment request submitted.";
        return new OkObjectResult(ApiResponse.Ok(dto, msg));
    }

    /// <summary>
    /// 新增 / 更新出差請款撥款分期明細。
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

        var tpr = await db.TravelPaymentRequests
                          .Include(t => t.Installments)
                          .FirstOrDefaultAsync(t => t.Id == intId)
                  ?? throw AppException.NotFound("TravelPaymentRequest");

        if (tpr.ApprovalStatus != "approved")
            return new BadRequestObjectResult(ApiResponse.Fail("只有已核准的出差請款申請可以設定撥款明細。"));

        var body = await req.ReadFromJsonAsync<UpsertInstallmentsRequest>(JsonOpts);
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        // 共用 validate + diff（不 SaveChanges）
        var newlyPaid = InstallmentUpsertService.Apply(
            db, tpr.Installments, body.Installments, tpr.GrandTotal, userId,
            () => new TravelPaymentRequestInstallment { TravelPaymentRequestId = tpr.Id });

        await db.SaveChangesAsync();

        if (tpr.EmployeeId.HasValue)
            foreach (var np in newlyPaid)
                await notifier.NotifyApplicantPaidAsync(
                    "travel_payment", tpr.Id, tpr.EmployeeId.Value, np.Amount, np.PaidAt,
                    installmentNo: np.InstallmentNo, totalInstallments: np.TotalInstallments);

        return new OkObjectResult(ApiResponse.Ok(
            new { tpr.Id, InstallmentCount = body.Installments.Count },
            $"已更新 {body.Installments.Count} 筆撥款明細。"));
    }

    // ── Helper ──────────────────────────────────────────────────────────────────

    private async Task<Guid> GetUserIdAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw AppException.Unauthorized("Invalid token claims.");
        return userId;
    }
}
