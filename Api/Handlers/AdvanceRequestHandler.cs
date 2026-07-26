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
                DesignatedReviewerHelper.BuildEntities("advance", ar.Id, designatedReviewers));
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

        EnsureNoSupplementInFlight(ar);

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
                    DesignatedReviewerHelper.BuildEntities("advance", intId, designatedReviewers));
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

        EnsureNoSupplementInFlight(ar);

        // 收集需清理的 blob
        var blobNames = ar.Items
            .Select(i => blob.ExtractBlobName(i.FileUrl, ContainerName))
            .Where(n => n is not null)
            .ToList();

        // 一併清除此申請單的審核流程足跡（多型關聯無 FK，須手動刪除，否則殘留列會擋住使用者刪除）
        db.ApprovalRecords.RemoveRange(
            await db.ApprovalRecords.Where(r => r.ApplicationType == "advance" && r.ApplicationId == ar.Id).ToListAsync());
        db.EscalationOverrides.RemoveRange(
            await db.EscalationOverrides.Where(o => o.ApplicationType == "advance" && o.ApplicationId == ar.Id).ToListAsync());
        db.RequestDesignatedReviewers.RemoveRange(
            await db.RequestDesignatedReviewers.Where(r => r.RequestType == "advance" && r.RequestId == ar.Id).ToListAsync());

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

        return await SubmitCoreAsync(ar, userId, isSupplementRound: false);
    }

    /// <summary>
    /// 送簽核心：退回重送與追加送簽共用。
    /// isSupplementRound = true 時代表本次送出的是新建立的追加批次（父單原本為 approved）。
    /// </summary>
    private async Task<IActionResult> SubmitCoreAsync(AdvanceRequest ar, Guid userId, bool isSupplementRound)
    {
        var roundNo = ar.CurrentRoundNo;

        // 退回重送 / 追加新輪次：清除「本輪」審核記錄，重置指定審核者狀態
        if (ar.ApprovalStatus == "returned" || isSupplementRound)
        {
            // 追加輪只刪本輪紀錄，第 1 輪（含更早批次）的簽核歷程必須保留
            var oldRecords = await db.ApprovalRecords
                .Where(r => r.ApplicationType == "advance" && r.ApplicationId == ar.Id
                         && (roundNo == 1 || r.RoundNo == roundNo))
                .ToListAsync();
            db.ApprovalRecords.RemoveRange(oldRecords);

            var oldOverrides = await db.EscalationOverrides
                .Where(o => o.ApplicationType == "advance" && o.ApplicationId == ar.Id)
                .ToListAsync();
            db.EscalationOverrides.RemoveRange(oldOverrides);

            // 重置指定審核者狀態為 pending
            await AdvanceSupplementService.ResetDesignatedReviewersAsync(db, ar.Id);
        }

        var roundSuffix = roundNo > 1 ? $"（第 {roundNo} 次追加）" : "";
        var submitter = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

        // 自動關聯簽核流程（依申請人部門挑流程：部門專屬優先，否則退回通用預設）
        if (ar.ApprovalItemId is null)
            ar.ApprovalItemId = await approvalFlow.ResolveApprovalItemIdAsync("advance", submitter?.DepartmentId);

        // Superadmin 直接自動核准
        if (submitter?.IsSuperAdmin == true)
        {
            ar.ApprovalStatus   = "approved";
            ar.CurrentStepOrder = 1;
            ar.ReviewedAt       = Clock.Now;
            ar.ReviewedById     = userId;
            ar.ReviewNote       = $"系統自動核准（Superadmin）{roundSuffix}";
            await db.SaveChangesAsync();
            var saDto = await reader.GetByIdAsync(ar.Id);
            return new OkObjectResult(ApiResponse.Ok(saDto, "Advance request auto-approved."));
        }

        // 正規化各 designee 所屬步驟並驗證每個指定審核步驟皆有審核者
        await DesignatedReviewerHelper.ValidateAndNormalizeAsync(db, "advance", ar.Id, ar.ApprovalItemId, userId);
        await db.SaveChangesAsync();

        // 查詢指定審核者清單傳給 ResolveStartingStepAsync（含 ApprovalStepOrder 綁定步驟）
        var designatedReviewers = await DesignatedReviewerHelper.ReadForFlowAsync(db, "advance", ar.Id);

        // 自審跳過邏輯（與請款一致，不升級）
        var (startStep, autoApproved, _) = await approvalFlow.ResolveStartingStepAsync(
            ar.ApprovalItemId, userId, "advance", designatedReviewers);

        if (autoApproved)
        {
            ar.ApprovalStatus   = "approved";
            ar.CurrentStepOrder = startStep;
            ar.ReviewedAt       = Clock.Now;
            ar.ReviewedById     = userId;
            ar.ReviewNote       = $"系統自動核准（所有審核步驟皆為申請人本人）{roundSuffix}";
        }
        else
        {
            ar.ApprovalStatus   = "pending";
            ar.CurrentStepOrder = startStep;
        }

        await db.SaveChangesAsync();

        if (!autoApproved && ar.SubmittedById.HasValue)
        {
            // 指定審核步驟（原生 UseApplicantDesignated 或例外指定審核命中）：讀 designee 快照，
            // 與 ResolveStartingStepAsync 的判定同源，確保不會誤走部門/職稱通知
            bool isDesignatedStep = designatedReviewers.Any(r => r.ApprovalStepOrder == startStep);
            if (isDesignatedStep)
            {
                var firstReviewer = await db.RequestDesignatedReviewers
                    .AsNoTracking()
                    .Where(r => r.RequestType == "advance" && r.RequestId == ar.Id
                             && r.ApprovalStepOrder == startStep && r.Status == "pending")
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

    /// <summary>
    /// 新增 / 更新預支撥款分期明細。
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

        var ar = await db.AdvanceRequests
                         .Include(a => a.Installments)
                         .FirstOrDefaultAsync(a => a.Id == intId)
                 ?? throw AppException.NotFound("AdvanceRequest");

        if (ar.ApprovalStatus != "approved")
            return new BadRequestObjectResult(ApiResponse.Fail("只有已核准的預支申請可以設定撥款明細。"));

        var body = await req.ReadFromJsonAsync<UpsertInstallmentsRequest>(JsonOpts);
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        // 共用 validate + diff（不 SaveChanges）
        var newlyPaid = InstallmentUpsertService.Apply(
            db, ar.Installments, body.Installments, ar.GrandTotal, userId,
            () => new AdvanceRequestInstallment { AdvanceRequestId = ar.Id });

        await db.SaveChangesAsync();

        if (ar.SubmittedById.HasValue)
            foreach (var np in newlyPaid)
                await notifier.NotifyApplicantPaidAsync(
                    "advance", ar.Id, ar.SubmittedById.Value, np.Amount, np.PaidAt,
                    installmentNo: np.InstallmentNo, totalInstallments: np.TotalInstallments);

        return new OkObjectResult(ApiResponse.Ok(
            new { ar.Id, InstallmentCount = body.Installments.Count },
            $"已更新 {body.Installments.Count} 筆撥款明細。"));
    }

    // ── 追加預支批次 ────────────────────────────────────────────────────────

    /// <summary>
    /// 新增追加預支批次並直接送簽（不設草稿階段）。
    /// 僅已核准且未結案、且無進行中追加的預支單可追加；追加明細併入父單總額後重跑同一份 advance 簽核流程。
    /// </summary>
    public async Task<IActionResult> CreateSupplementAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advance request ID format."));

        var ar = await LoadOwnedAsync(intId, userId);

        if (ar.ApprovalStatus != "approved")
            throw AppException.BadRequest("只有已核准的預支申請可以新增追加預支。");
        if (ar.IsClosed)
            throw AppException.BadRequest("此預支申請已結案，無法新增追加預支。");

        var form = await req.ReadFormAsync();
        var advanceDateStr = form["advanceDate"].ToString();
        var reason         = form["reason"].ToString();
        var itemsJson      = form["items"].ToString();

        if (!DateTime.TryParse(advanceDateStr, out var advanceDate))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid advanceDate."));

        var itemsMeta = JsonSerializer.Deserialize<ItemMetadata[]>(itemsJson, JsonOpts);
        if (itemsMeta is null || itemsMeta.Length == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("At least one item is required."));

        var roundNo = ar.CurrentRoundNo + 1;

        // 快照父單目前的核准狀態，供追加被駁回時回滾
        db.AdvanceRequestSupplements.Add(new AdvanceRequestSupplement
        {
            AdvanceRequestId     = ar.Id,
            RoundNo              = roundNo,
            AdvanceDate          = advanceDate,
            Reason               = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CreatedById          = userId,
            CreatedAt            = Clock.Now,
            PrevCurrentStepOrder = ar.CurrentStepOrder,
            PrevReviewedAt       = ar.ReviewedAt,
            PrevReviewedById     = ar.ReviewedById,
            PrevReviewNote       = ar.ReviewNote,
        });

        var newItems = await BuildItemsAsync(form, itemsMeta, ar.Id, roundNo);
        db.AdvanceRequestItems.AddRange(newItems);

        ar.CurrentRoundNo = roundNo;
        // 以 RoundNo 過濾既有明細：AddRange 後 EF 會把 newItems 修補進 ar.Items，直接 Concat 會重複計算
        RecomputeTotals(ar, ar.Items.Where(i => i.RoundNo != roundNo).Concat(newItems));

        await db.SaveChangesAsync();

        // 併入總額後直接送簽，走同一份 advance 簽核流程
        return await SubmitCoreAsync(ar, userId, isSupplementRound: true);
    }

    /// <summary>追加批次被退回後編輯該批次明細（不送簽，前端接著呼叫 PATCH /{id}/submit 重送）。</summary>
    public async Task<IActionResult> UpdateSupplementAsync(HttpRequest req, string id, string round)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId) || !int.TryParse(round, out var roundNo))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid ID format."));

        var ar = await LoadOwnedAsync(intId, userId);
        var supplement = await EnsureEditableSupplementAsync(ar, roundNo);

        var form = await req.ReadFormAsync();
        var advanceDateStr = form["advanceDate"].ToString();
        var reason         = form["reason"].ToString();
        var itemsJson      = form["items"].ToString();

        if (DateTime.TryParse(advanceDateStr, out var advanceDate))
            supplement.AdvanceDate = advanceDate;
        if (form.ContainsKey("reason"))
            supplement.Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        var itemsMeta = JsonSerializer.Deserialize<ItemMetadata[]>(itemsJson, JsonOpts);
        if (itemsMeta is null || itemsMeta.Length == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("At least one item is required."));

        // 只替換本批次明細；blob 差集僅在本批次內比對，避免誤刪其他批次的檔案
        var roundItems = ar.Items.Where(i => i.RoundNo == roundNo).ToList();
        var oldFileUrls = roundItems
            .Where(i => !string.IsNullOrEmpty(i.FileUrl))
            .Select(i => i.FileUrl!)
            .ToHashSet();
        db.AdvanceRequestItems.RemoveRange(roundItems);

        var newItems = await BuildItemsAsync(form, itemsMeta, ar.Id, roundNo);
        db.AdvanceRequestItems.AddRange(newItems);

        // 同上：只取其他批次的明細再併本批次新明細，避免 EF fixup 造成重複計算
        var keptItems = ar.Items.Where(i => i.RoundNo != roundNo).ToList();
        RecomputeTotals(ar, keptItems.Concat(newItems));

        await db.SaveChangesAsync();

        var newFileUrls = newItems.Where(i => !string.IsNullOrEmpty(i.FileUrl)).Select(i => i.FileUrl!).ToHashSet();
        foreach (var oldUrl in oldFileUrls.Except(newFileUrls))
        {
            var blobName = blob.ExtractBlobName(oldUrl, ContainerName);
            if (blobName is not null)
                await blob.DeleteAsync(ContainerName, blobName);
        }

        var dto = await reader.GetByIdAsync(ar.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Advance supplement updated."));
    }

    /// <summary>放棄追加批次：把父單還原成送出追加之前的已核准狀態。</summary>
    public async Task<IActionResult> DeleteSupplementAsync(HttpRequest req, string id, string round)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId) || !int.TryParse(round, out var roundNo))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid ID format."));

        var ar = await LoadOwnedAsync(intId, userId);
        await EnsureEditableSupplementAsync(ar, roundNo);

        var blobNames = await AdvanceSupplementService.RollbackAsync(db, blob, ar);
        await db.SaveChangesAsync();
        await AdvanceSupplementService.DeleteBlobsAsync(blob, blobNames);

        var dto = await reader.GetByIdAsync(ar.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, $"第 {roundNo} 次追加預支已取消，原預支申請維持核准。"));
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private async Task<AdvanceRequest> LoadOwnedAsync(int intId, Guid userId)
    {
        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var ar = currentUser?.IsSuperAdmin == true
            ? await db.AdvanceRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId)
            : await db.AdvanceRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == intId && x.SubmittedById == userId);
        return ar ?? throw AppException.NotFound("AdvanceRequest");
    }

    /// <summary>有進行中的追加批次時，禁止整單編輯 / 刪除（否則會動到已核准甚至已撥款的原始明細）。</summary>
    private static void EnsureNoSupplementInFlight(AdvanceRequest ar)
    {
        if (ar.CurrentRoundNo > 1 && (ar.ApprovalStatus == "pending" || ar.ApprovalStatus == "returned"))
            throw AppException.BadRequest("此預支申請有進行中的追加批次，請先處理追加批次。");
    }

    private async Task<AdvanceRequestSupplement> EnsureEditableSupplementAsync(AdvanceRequest ar, int roundNo)
    {
        if (ar.ApprovalStatus != "returned" || roundNo != ar.CurrentRoundNo)
            throw AppException.BadRequest("只有被退回的最新追加批次可以編輯或取消。");

        return await db.AdvanceRequestSupplements
                   .FirstOrDefaultAsync(s => s.AdvanceRequestId == ar.Id && s.RoundNo == roundNo)
               ?? throw AppException.NotFound("AdvanceRequestSupplement");
    }

    /// <summary>由 multipart 的 items JSON + files 建立指定批次的明細（含 Blob 上傳）。</summary>
    private async Task<List<AdvanceRequestItem>> BuildItemsAsync(
        IFormCollection form, ItemMetadata[] itemsMeta, int advanceRequestId, int roundNo)
    {
        var files = form.Files.GetFiles("files");
        var result = new List<AdvanceRequestItem>();

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

            result.Add(new AdvanceRequestItem
            {
                AdvanceRequestId = advanceRequestId,
                RoundNo     = roundNo,
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

        return result;
    }

    private static void RecomputeTotals(AdvanceRequest ar, IEnumerable<AdvanceRequestItem> items)
    {
        var list = items.ToList();
        ar.CashTotal  = list.Sum(i => i.CashAmount);
        ar.CheckTotal = list.Sum(i => i.CheckAmount);
        ar.GrandTotal = list.Sum(i => i.TotalPrice);
    }

    private async Task<Guid> GetUserIdAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw AppException.Unauthorized("Invalid token claims.");
        return userId;
    }
}
