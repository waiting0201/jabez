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
/// PATCH  /travel-payment-requests/{id}/payment-date → 更新撥款日（僅財務部/Superadmin）
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

        // 產生出差請款單號：TPR-yyyyMMdd-NNN（唯一索引保護並發）
        var today = Clock.Now;
        var prefix = $"TPR-{today:yyyyMMdd}-";
        var maxNo = await db.TravelPaymentRequests
            .Where(t => t.RequestNo.StartsWith(prefix))
            .MaxAsync(t => (string?)t.RequestNo);
        int seq = 1;
        if (maxNo is not null)
        {
            var seqStr = maxNo[prefix.Length..];
            if (int.TryParse(seqStr, out var parsed))
                seq = parsed + 1;
        }
        var requestNo = $"{prefix}{seq:D3}";

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
            RequestNo       = requestNo,
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
                designatedReviewers.OrderBy(r => r.StepOrder).Select(r => new RequestDesignatedReviewer
                {
                    RequestType = AppType,
                    RequestId   = request.Id,
                    ReviewerId  = r.ReviewerId,
                    StepOrder   = r.StepOrder,
                }));
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
        if (form.ContainsKey("startDate") && DateTime.TryParse(form["startDate"], out var startDate))
            item.StartDate = startDate;
        if (form.ContainsKey("endDate") && DateTime.TryParse(form["endDate"], out var endDate))
            item.EndDate = endDate;
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
                        updateDesignatedReviewers.Select(r => new RequestDesignatedReviewer
                        {
                            RequestType = AppType,
                            RequestId   = intId,
                            ReviewerId  = r.ReviewerId,
                            StepOrder   = r.StepOrder,
                        }));
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

        // 自動關聯簽核流程（依 ApplicationType 查找啟用的流程）
        if (item.ApprovalItemId is null)
        {
            var flow = await db.ApprovalItems
                .AsNoTracking()
                .FirstOrDefaultAsync(ai => ai.ApplicationType == AppType && ai.IsActive);
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
                    .AnyAsync(r => r.RequestType == AppType && r.RequestId == item.Id);
                if (!hasReviewers)
                    return new BadRequestObjectResult(ApiResponse.Fail("此簽核流程包含申請人指定審核步驟，請提供指定審核者。"));
            }
        }

        var designatedReviewers = await db.RequestDesignatedReviewers
            .AsNoTracking()
            .Where(r => r.RequestType == AppType && r.RequestId == item.Id)
            .OrderBy(r => r.StepOrder)
            .Select(r => new DesignatedReviewerRequest(r.ReviewerId, r.StepOrder))
            .ToListAsync();

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
                bool isDesignatedStep = item.ApprovalItemId.HasValue && await db.ApprovalSteps.AsNoTracking()
                    .AnyAsync(s => s.ApprovalItemId == item.ApprovalItemId
                        && s.StepOrder == startStep
                        && s.UseApplicantDesignated);
                if (isDesignatedStep)
                {
                    var firstReviewer = await db.RequestDesignatedReviewers
                        .AsNoTracking()
                        .Where(r => r.RequestType == AppType && r.RequestId == item.Id && r.Status == "pending")
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

    /// <summary>更新撥款日（僅財務部/Superadmin）</summary>
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

        var tpr = await db.TravelPaymentRequests.FindAsync(intId)
            ?? throw AppException.NotFound("TravelPaymentRequest");

        if (tpr.ApprovalStatus != "approved")
            return new BadRequestObjectResult(ApiResponse.Fail("只有已核准的出差請款申請可以設定撥款日。"));

        var body = await req.ReadFromJsonAsync<UpdatePaymentDateRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        // 偵測撥款狀態轉換（null → 有值）
        var wasPaidNull = !tpr.PaidAt.HasValue;

        if (body.EstimatedPaymentDate.HasValue)
            tpr.EstimatedPaymentDate = body.EstimatedPaymentDate.Value;
        if (body.PaidAt.HasValue)
        {
            tpr.PaidAt        = body.PaidAt.Value;
            tpr.PaidByUserId  = userId;
        }

        await db.SaveChangesAsync();

        // 首次撥款（null → 有值）→ 通知申請人
        if (wasPaidNull && tpr.PaidAt.HasValue && tpr.EmployeeId.HasValue)
            await notifier.NotifyApplicantPaidAsync(
                "travel_payment", tpr.Id, tpr.EmployeeId.Value, tpr.GrandTotal, tpr.PaidAt.Value);

        return new OkObjectResult(ApiResponse.Ok(
            new { tpr.Id, tpr.EstimatedPaymentDate, tpr.PaidAt },
            "撥款日期已更新。"));
    }

    /// <summary>
    /// 新增 / 更新出差請款撥款分期明細（同步維護父表 cache）。
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

        var existingSnap = tpr.Installments
            .Select(i => (i.Id, i.InstallmentNo, i.ExpectedDate, i.PaidAt, i.Amount))
            .ToList();
        InstallmentValidator.Validate(body.Installments, tpr.GrandTotal, existingSnap);

        var nowUtc = DateTime.UtcNow;
        var taipeiTz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
        var nowTaipei = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, taipeiTz);
        var newlyPaid = new List<NewlyPaidInstallment>();
        var inputIds = body.Installments.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();

        var toRemove = tpr.Installments.Where(e => !inputIds.Contains(e.Id)).ToList();
        foreach (var r in toRemove)
            db.TravelPaymentRequestInstallments.Remove(r);

        foreach (var input in body.Installments)
        {
            if (input.Id.HasValue)
            {
                var existing = tpr.Installments.FirstOrDefault(e => e.Id == input.Id.Value)
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
                var ins = new TravelPaymentRequestInstallment
                {
                    TravelPaymentRequestId = tpr.Id,
                    InstallmentNo          = input.InstallmentNo,
                    ExpectedDate           = input.ExpectedDate.Date,
                    Amount                 = input.Amount,
                    Note                   = input.Note,
                    CreatedAt              = nowUtc,
                    UpdatedAt              = nowUtc,
                };
                if (input.PaidAt.HasValue)
                {
                    ins.PaidAt = input.PaidAt.Value.Date + nowTaipei.TimeOfDay;
                    ins.PaidByUserId = userId;
                    newlyPaid.Add(new(ins.InstallmentNo, ins.PaidAt.Value, ins.Amount, body.Installments.Count));
                }
                db.TravelPaymentRequestInstallments.Add(ins);
            }
        }

        var cacheInput = body.Installments
            .Select(i => (i.ExpectedDate, PaidAt: i.PaidAt.HasValue ? i.PaidAt.Value.Date + nowTaipei.TimeOfDay : (DateTime?)null))
            .ToList();
        var (cacheEstimated, cachePaidAt, _) = InstallmentValidator.ComputeCache(cacheInput);
        tpr.EstimatedPaymentDate = cacheEstimated;
        tpr.PaidAt = cachePaidAt;
        tpr.PaidByUserId = cachePaidAt.HasValue ? userId : null;

        await db.SaveChangesAsync();

        if (tpr.EmployeeId.HasValue)
            foreach (var np in newlyPaid)
                await notifier.NotifyApplicantPaidAsync(
                    "travel_payment", tpr.Id, tpr.EmployeeId.Value, np.Amount, np.PaidAt,
                    installmentNo: np.InstallmentNo, totalInstallments: np.TotalInstallments);

        return new OkObjectResult(ApiResponse.Ok(
            new { tpr.Id, tpr.EstimatedPaymentDate, tpr.PaidAt, InstallmentCount = body.Installments.Count },
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
