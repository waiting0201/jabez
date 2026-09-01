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
/// 員工人事資料卡 Handler。
/// GET  /users/{id}/profile → 讀取完整人事資料卡（Dapper，一次 QueryMultiple）
/// PUT  /users/{id}/profile → 儲存人事資料卡（multipart，9 子表整批替換）
/// </summary>
public sealed class EmployeeProfileHandler(
    AppDbContext db,
    IEmployeeProfileReadService reader,
    IBlobStorageService blob)
{
    private const string IdCardContainer = "id-cards";
    private static readonly string[] AllowedIdCardTypes = ["image/png", "image/jpeg", "application/pdf"];

    private const string EducationProofContainer = "education-proofs";
    private static readonly string[] AllowedEducationProofTypes = ["image/png", "image/jpeg", "application/pdf"];

    private const string PassbookContainer = "passbooks";
    private static readonly string[] AllowedPassbookTypes = ["image/png", "image/jpeg", "application/pdf"];

    // 寬鬆日期解析：Safari 不支援 <input type="month">，學歷等年月欄位可能為手打字串（見 FlexibleDateTimeJsonConverter）
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new FlexibleDateTimeConverter(), new FlexibleNullableDateTimeConverter() },
    };

    // GET /me/profile → 員工自助查詢自己的人事資料卡（登入即可，不需 users:read）
    public async Task<IActionResult> GetMineAsync(HttpRequest req)
    {
        var userIdStr = req.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return new UnauthorizedObjectResult(ApiResponse.Fail("Unauthorized.", "Invalid token claims."));

        // 自助端點允許員工讀自己的人事資料卡；Superadmin 讀自己也無妨（其人事資料卡通常不存在，回 null 即可）
        // 刻意不套薪資欄位級權限（PayrollFieldAccess）—— 員工看自己的薪資調整歷史是既有需求，
        // 該權限只管「看別人的」。請勿為了與 GetByUserIdAsync 一致而補上。
        var dto = await reader.GetByUserIdAsync(userId);
        return new OkObjectResult(ApiResponse.Ok(dto));
    }

    // GET /users/{id}/profile
    public async Task<IActionResult> GetByUserIdAsync(HttpRequest req, string id)
    {
        if (!Guid.TryParse(id, out var userId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid user ID format."));

        // 不可讀取 Superadmin 的人事資料卡（Superadmin 不是真實員工）
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            throw AppException.NotFound("User");
        if (user.IsSuperAdmin)
            throw AppException.Forbidden("Cannot access the system super admin account.");

        var dto = await reader.GetByUserIdAsync(userId);

        // 欄位級權限：無 payroll:read 者不回傳薪資調整歷史（整張表皆為薪資原料）
        if (dto is not null && !PayrollFieldAccess.CanSeeSalary(req.HttpContext.User))
            dto = PayrollFieldAccess.Mask(dto);

        return new OkObjectResult(ApiResponse.Ok(dto));
    }

    // PUT /users/{id}/profile
    public async Task<IActionResult> UpsertAsync(HttpRequest req, string id)
    {
        if (!Guid.TryParse(id, out var userId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid user ID format."));

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw AppException.NotFound("User");

        if (user.IsSuperAdmin)
            throw AppException.Forbidden("Cannot modify the system super admin account.");

        var form = await req.ReadFormAsync();

        // 解析 JSON payload（text part "payload"）
        var payloadJson = form["payload"].ToString();
        if (string.IsNullOrWhiteSpace(payloadJson))
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的請求內容 (payload)。"));

        EmployeeProfileUpsertRequest payload;
        try
        {
            payload = JsonSerializer.Deserialize<EmployeeProfileUpsertRequest>(payloadJson, PayloadJsonOptions)
                ?? throw new JsonException("payload is null");
        }
        catch (JsonException ex)
        {
            // 帶回欄位層級細節（如「無法解析日期格式：xxx」+ JSON path），方便前端 / 使用者定位問題欄位
            return new BadRequestObjectResult(ApiResponse.Fail("請求內容格式不正確。", ex.Message));
        }

        // 薪資欄位級權限：只有「看得到（payroll:read）」且「有送」才動薪資子表。
        // 整批替換模式下送 [] 等於清空全部薪資調整歷史，而該歷史是 User 7 個薪資欄的唯一真實來源，
        // 清掉不可還原 —— 無權者的前端不送此 key，這裡一律當作「不變更」。
        var touchSalary = PayrollFieldAccess.CanSeeSalary(req.HttpContext.User)
                          && payload.SalaryAdjustmentRecords is not null;

        // 使用 ExecutionStrategy 包裝 transaction：DbContext 啟用 EnableRetryOnFailure
        // 後直接呼叫 BeginTransactionAsync 會被阻擋，須透過 strategy 執行整批替換的原子性操作。
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            // ── 1. Upsert EmployeeProfile（主表）─────────────────────────────
            var profile = await db.EmployeeProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            bool isNew  = profile is null;
            profile ??= new EmployeeProfile { UserId = userId };

            profile.EmployeeNumber        = payload.EmployeeNumber;
            profile.EnglishName           = payload.EnglishName;
            profile.IdNumber              = payload.IdNumber;
            profile.Gender                = payload.Gender;
            profile.MaritalStatus         = payload.MaritalStatus;
            profile.BirthPlace            = payload.BirthPlace;
            profile.MobilePhone           = payload.MobilePhone;
            profile.ResidentialAddress    = payload.ResidentialAddress;
            profile.ResidentialPhone      = payload.ResidentialPhone;
            profile.MailingAddress        = payload.MailingAddress;
            profile.MailingPhone          = payload.MailingPhone;
            profile.EmergencyContactName  = payload.EmergencyContactName;
            profile.EmergencyContactPhone = payload.EmergencyContactPhone;
            profile.BankCode              = payload.BankCode;
            profile.BankAccount           = payload.BankAccount;
            profile.BankCode2             = payload.BankCode2;
            profile.BankAccount2          = payload.BankAccount2;
            profile.InsuranceStartDate    = payload.InsuranceStartDate;
            profile.DependentCount        = payload.DependentCount;
            profile.Specialties           = payload.Specialties;
            profile.ResignationReason     = payload.ResignationReason;
            profile.UpdatedAt             = Clock.Now;

            // ── 2. 身分證正面 Blob 處理 ─────────────────────────────────────
            if (form["removeIdCardFront"] == "true")
            {
                await TryDeleteBlobByUrlAsync(IdCardContainer, profile.IdCardFrontUrl);
                profile.IdCardFrontUrl = null;
            }
            else
            {
                var frontFile = form.Files.GetFile("idCardFront");
                if (frontFile is not null && frontFile.Length > 0)
                {
                    if (frontFile.Length > 1 * 1024 * 1024)
                        throw AppException.BadRequest("上傳照片勿超過1MB");

                    string? actualType;
                    using (var peek = frontFile.OpenReadStream())
                        actualType = await FileSignatureValidator.DetectAsync(peek);

                    if (actualType is null || !AllowedIdCardTypes.Contains(actualType))
                        throw AppException.BadRequest("身分證正面影本僅支援 PNG、JPEG 圖片或 PDF 格式。");

                    var ext      = Path.GetExtension(frontFile.FileName);
                    var blobName = $"{userId}_front{ext}";
                    var newUrl   = $"files/{IdCardContainer}/{blobName}";

                    using (var stream = frontFile.OpenReadStream())
                        await blob.UploadAsync(IdCardContainer, blobName, stream, actualType);

                    if (!string.Equals(profile.IdCardFrontUrl, newUrl, StringComparison.OrdinalIgnoreCase))
                        await TryDeleteBlobByUrlAsync(IdCardContainer, profile.IdCardFrontUrl);

                    profile.IdCardFrontUrl = newUrl;
                }
            }

            // ── 3. 身分證背面 Blob 處理 ─────────────────────────────────────
            if (form["removeIdCardBack"] == "true")
            {
                await TryDeleteBlobByUrlAsync(IdCardContainer, profile.IdCardBackUrl);
                profile.IdCardBackUrl = null;
            }
            else
            {
                var backFile = form.Files.GetFile("idCardBack");
                if (backFile is not null && backFile.Length > 0)
                {
                    if (backFile.Length > 1 * 1024 * 1024)
                        throw AppException.BadRequest("上傳照片勿超過1MB");

                    string? actualType;
                    using (var peek = backFile.OpenReadStream())
                        actualType = await FileSignatureValidator.DetectAsync(peek);

                    if (actualType is null || !AllowedIdCardTypes.Contains(actualType))
                        throw AppException.BadRequest("身分證背面影本僅支援 PNG、JPEG 圖片或 PDF 格式。");

                    var ext      = Path.GetExtension(backFile.FileName);
                    var blobName = $"{userId}_back{ext}";
                    var newUrl   = $"files/{IdCardContainer}/{blobName}";

                    using (var stream = backFile.OpenReadStream())
                        await blob.UploadAsync(IdCardContainer, blobName, stream, actualType);

                    if (!string.Equals(profile.IdCardBackUrl, newUrl, StringComparison.OrdinalIgnoreCase))
                        await TryDeleteBlobByUrlAsync(IdCardContainer, profile.IdCardBackUrl);

                    profile.IdCardBackUrl = newUrl;
                }
            }

            // ── 4. 最高學歷證明 Blob 處理 ─────────────────────────────────────
            if (form["removeHighestEducationProof"] == "true")
            {
                await TryDeleteBlobByUrlAsync(EducationProofContainer, profile.HighestEducationProofUrl);
                profile.HighestEducationProofUrl = null;
            }
            else
            {
                var proofFile = form.Files.GetFile("highestEducationProof");
                if (proofFile is not null && proofFile.Length > 0)
                {
                    if (proofFile.Length > 1 * 1024 * 1024)
                        throw AppException.BadRequest("上傳照片勿超過1MB");

                    string? actualType;
                    using (var peek = proofFile.OpenReadStream())
                        actualType = await FileSignatureValidator.DetectAsync(peek);

                    if (actualType is null || !AllowedEducationProofTypes.Contains(actualType))
                        throw AppException.BadRequest("最高學歷證明僅支援 PNG、JPEG 圖片或 PDF 格式。");

                    var ext      = Path.GetExtension(proofFile.FileName);
                    var blobName = $"{userId}_education{ext}";
                    var newUrl   = $"files/{EducationProofContainer}/{blobName}";

                    using (var stream = proofFile.OpenReadStream())
                        await blob.UploadAsync(EducationProofContainer, blobName, stream, actualType);

                    if (!string.Equals(profile.HighestEducationProofUrl, newUrl, StringComparison.OrdinalIgnoreCase))
                        await TryDeleteBlobByUrlAsync(EducationProofContainer, profile.HighestEducationProofUrl);

                    profile.HighestEducationProofUrl = newUrl;
                }
            }

            // ── 5. 存摺封面 Blob 處理（第一 / 第二帳戶各一張，欄位與 blob 名稱對稱）──
            profile.BankBookImageUrl  = await ProcessPassbookAsync(
                form, userId, "bankBookImage",  "removeBankBook",  "passbook",  profile.BankBookImageUrl);
            profile.BankBookImageUrl2 = await ProcessPassbookAsync(
                form, userId, "bankBookImage2", "removeBankBook2", "passbook2", profile.BankBookImageUrl2);

            if (isNew)
                db.EmployeeProfiles.Add(profile);

            // ── 4. 9 個子表：整批替換（先 DELETE 舊資料，再 INSERT 新資料）────
            // 效能考量：EF ExecuteDeleteAsync 直接 DELETE WHERE UserId = @id，不需 tracking
            // 例外：薪資調整歷史為「條件式」整批替換，受 touchSalary 控管（見上方註解）
            await db.EducationRecords.Where(e => e.UserId == userId).ExecuteDeleteAsync();
            await db.EmploymentHistoryRecords.Where(e => e.UserId == userId).ExecuteDeleteAsync();
            await db.FamilyMembers.Where(f => f.UserId == userId).ExecuteDeleteAsync();
            await db.ProfessionalTrainings.Where(p => p.UserId == userId).ExecuteDeleteAsync();
            await db.LanguageAbilities.Where(l => l.UserId == userId).ExecuteDeleteAsync();
            await db.JobTransferRecords.Where(j => j.UserId == userId).ExecuteDeleteAsync();
            await db.RewardPunishmentRecords.Where(r => r.UserId == userId).ExecuteDeleteAsync();
            if (touchSalary)
                await db.SalaryAdjustmentRecords.Where(s => s.UserId == userId).ExecuteDeleteAsync();
            await db.HealthInsuranceDependents.Where(h => h.UserId == userId).ExecuteDeleteAsync();

            var now = Clock.Now;

            db.EducationRecords.AddRange(payload.EducationRecords.Select(r => new EducationRecord
            {
                Id = Guid.NewGuid(), UserId = userId,
                School = r.School, Department = r.Department, Degree = r.Degree,
                StartDate = r.StartDate, EndDate = r.EndDate, Order = r.Order,
                CreatedAt = now, UpdatedAt = now
            }));

            db.EmploymentHistoryRecords.AddRange(payload.EmploymentHistoryRecords.Select(r => new EmploymentHistoryRecord
            {
                Id = Guid.NewGuid(), UserId = userId,
                Organization = r.Organization, JobTitle = r.JobTitle,
                StartDate = r.StartDate, EndDate = r.EndDate, Order = r.Order,
                CreatedAt = now, UpdatedAt = now
            }));

            db.FamilyMembers.AddRange(payload.FamilyMembers.Select(r => new FamilyMember
            {
                Id = Guid.NewGuid(), UserId = userId,
                Name = r.Name, Relationship = r.Relationship,
                Age = r.Age, Occupation = r.Occupation,
                CreatedAt = now, UpdatedAt = now
            }));

            db.ProfessionalTrainings.AddRange(payload.ProfessionalTrainings.Select(r => new ProfessionalTraining
            {
                Id = Guid.NewGuid(), UserId = userId,
                TrainingName = r.TrainingName, TrainingOrg = r.TrainingOrg,
                StartDate = r.StartDate, EndDate = r.EndDate, Hours = r.Hours,
                CreatedAt = now, UpdatedAt = now
            }));

            db.LanguageAbilities.AddRange(payload.LanguageAbilities.Select(r => new LanguageAbility
            {
                Id = Guid.NewGuid(), UserId = userId,
                Language = r.Language,
                Listening = r.Listening, Speaking = r.Speaking,
                Reading = r.Reading, Writing = r.Writing,
                CreatedAt = now, UpdatedAt = now
            }));

            db.JobTransferRecords.AddRange(payload.JobTransferRecords.Select(r => new JobTransferRecord
            {
                Id = Guid.NewGuid(), UserId = userId,
                EffectiveDate = r.EffectiveDate,
                FromDepartment = r.FromDepartment, ToDepartment = r.ToDepartment,
                FromJobTitle = r.FromJobTitle, ToJobTitle = r.ToJobTitle,
                CreatedAt = now, UpdatedAt = now
            }));

            db.RewardPunishmentRecords.AddRange(payload.RewardPunishmentRecords.Select(r => new RewardPunishmentRecord
            {
                Id = Guid.NewGuid(), UserId = userId,
                EffectiveDate = r.EffectiveDate, Type = r.Type,
                Category = r.Category, Count = r.Count, Reason = r.Reason,
                CreatedAt = now, UpdatedAt = now
            }));

            List<SalaryAdjustmentRecord> salaryEntities = [];
            if (touchSalary)
            {
                salaryEntities = payload.SalaryAdjustmentRecords!.Select(r => new SalaryAdjustmentRecord
                {
                    Id = Guid.NewGuid(), UserId = userId,
                    EffectiveDate        = r.EffectiveDate,
                    BaseSalary           = r.BaseSalary,
                    OtherAllowance       = r.OtherAllowance,
                    AdjustmentDifference = r.AdjustmentDifference,
                    MealAllowance        = r.MealAllowance,
                    TotalAmount          = r.TotalAmount,
                    Notes                = r.Notes,
                    CreatedAt = now, UpdatedAt = now
                }).ToList();

                db.SalaryAdjustmentRecords.AddRange(salaryEntities);
            }

            db.HealthInsuranceDependents.AddRange(payload.HealthInsuranceDependents.Select(r => new HealthInsuranceDependent
            {
                Id = Guid.NewGuid(), UserId = userId,
                Name = r.Name, Relationship = r.Relationship,
                IdNumber = r.IdNumber, BirthDate = r.BirthDate,
                CreatedAt = now, UpdatedAt = now
            }));

            await db.SaveChangesAsync();

            // ── 5. 薪資同步：找 EffectiveDate <= 今日（Asia/Taipei）的最新薪資紀錄 ──
            // 取得 EffectiveDate 最大（最新有效）的薪資調整紀錄，同步至 User 的 4 個薪資欄位
            // （底薪 + 伙食費 + 其他加給 + 代扣代付款）；無符合不變。未動薪資子表時（touchSalary = false）整段跳過。
            var today = Clock.Now.Date;
            var latestSalary = salaryEntities
                .Where(s => s.EffectiveDate.Date <= today)
                .OrderByDescending(s => s.EffectiveDate)
                .FirstOrDefault();

            if (touchSalary && latestSalary is not null)
            {
                user.BaseSalary           = latestSalary.BaseSalary;
                user.MealAllowance        = latestSalary.MealAllowance;
                user.OtherAllowance       = latestSalary.OtherAllowance;
                user.AdjustmentDifference = latestSalary.AdjustmentDifference;
                user.UpdatedAt            = now;
                await db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            // await using var tx：未 Commit 即離開 scope（含例外）會自動 Rollback。
        });

        var dto = await reader.GetByUserIdAsync(userId);
        return new OkObjectResult(ApiResponse.Ok(dto, "人事資料卡已更新。"));
    }

    /// <summary>嘗試刪除 Blob；失敗只記錄，不阻斷主流程（孤兒檔案可事後清理）。</summary>
    /// <summary>
    /// 存摺封面上傳 / 移除（第一、第二帳戶共用）。
    /// 回傳處理後應存回 entity 的 URL：移除 → null、有上傳 → 新 URL、皆無 → 原值不變。
    /// blobSuffix 區隔兩個帳戶的 blob 名稱（passbook / passbook2），避免第二張覆蓋第一張。
    /// </summary>
    private async Task<string?> ProcessPassbookAsync(
        IFormCollection form, Guid userId,
        string fileKey, string removeKey, string blobSuffix, string? currentUrl)
    {
        if (form[removeKey] == "true")
        {
            await TryDeleteBlobByUrlAsync(PassbookContainer, currentUrl);
            return null;
        }

        var bankBookFile = form.Files.GetFile(fileKey);
        if (bankBookFile is null || bankBookFile.Length == 0)
            return currentUrl;

        if (bankBookFile.Length > 1 * 1024 * 1024)
            throw AppException.BadRequest("上傳照片勿超過1MB");

        string? actualType;
        using (var peek = bankBookFile.OpenReadStream())
            actualType = await FileSignatureValidator.DetectAsync(peek);

        if (actualType is null || !AllowedPassbookTypes.Contains(actualType))
            throw AppException.BadRequest("存摺封面僅支援 PNG、JPEG 圖片或 PDF 格式。");

        var ext      = Path.GetExtension(bankBookFile.FileName);
        var blobName = $"{userId}_{blobSuffix}{ext}";
        var newUrl   = $"files/{PassbookContainer}/{blobName}";

        using (var stream = bankBookFile.OpenReadStream())
            await blob.UploadAsync(PassbookContainer, blobName, stream, actualType);

        // 副檔名換掉時（jpg → pdf）新舊 URL 不同，須刪掉舊 blob 避免殘留
        if (!string.Equals(currentUrl, newUrl, StringComparison.OrdinalIgnoreCase))
            await TryDeleteBlobByUrlAsync(PassbookContainer, currentUrl);

        return newUrl;
    }

    private async Task TryDeleteBlobByUrlAsync(string container, string? url)
    {
        if (string.IsNullOrEmpty(url)) return;
        try
        {
            var proxyPrefix = $"files/{container}/";
            var blobName = url.StartsWith(proxyPrefix, StringComparison.OrdinalIgnoreCase)
                ? url[proxyPrefix.Length..]
                : blob.ExtractBlobName(url, container);
            if (blobName is not null)
                await blob.DeleteAsync(container, blobName);
        }
        catch (Exception)
        {
            // 孤兒檔案：可接受，不影響主流程
        }
    }
}
