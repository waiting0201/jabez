using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            payload = JsonSerializer.Deserialize<EmployeeProfileUpsertRequest>(payloadJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new JsonException("payload is null");
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(ApiResponse.Fail("請求內容格式不正確。"));
        }

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

            if (isNew)
                db.EmployeeProfiles.Add(profile);

            // ── 4. 9 個子表：整批替換（先 DELETE 舊資料，再 INSERT 新資料）────
            // 效能考量：EF ExecuteDeleteAsync 直接 DELETE WHERE UserId = @id，不需 tracking
            await db.EducationRecords.Where(e => e.UserId == userId).ExecuteDeleteAsync();
            await db.EmploymentHistoryRecords.Where(e => e.UserId == userId).ExecuteDeleteAsync();
            await db.FamilyMembers.Where(f => f.UserId == userId).ExecuteDeleteAsync();
            await db.ProfessionalTrainings.Where(p => p.UserId == userId).ExecuteDeleteAsync();
            await db.LanguageAbilities.Where(l => l.UserId == userId).ExecuteDeleteAsync();
            await db.JobTransferRecords.Where(j => j.UserId == userId).ExecuteDeleteAsync();
            await db.RewardPunishmentRecords.Where(r => r.UserId == userId).ExecuteDeleteAsync();
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

            var salaryEntities = payload.SalaryAdjustmentRecords.Select(r => new SalaryAdjustmentRecord
            {
                Id = Guid.NewGuid(), UserId = userId,
                EffectiveDate        = r.EffectiveDate,
                BaseSalary           = r.BaseSalary,
                PositionAllowance    = r.PositionAllowance,
                DutyAllowance        = r.DutyAllowance,
                OtherAllowance       = r.OtherAllowance,
                AdjustmentDifference = r.AdjustmentDifference,
                OverseasAllowance    = r.OverseasAllowance,
                MealAllowance        = r.MealAllowance,
                TotalAmount          = r.TotalAmount,
                Notes                = r.Notes,
                CreatedAt = now, UpdatedAt = now
            }).ToList();

            db.SalaryAdjustmentRecords.AddRange(salaryEntities);

            db.HealthInsuranceDependents.AddRange(payload.HealthInsuranceDependents.Select(r => new HealthInsuranceDependent
            {
                Id = Guid.NewGuid(), UserId = userId,
                Name = r.Name, Relationship = r.Relationship,
                IdNumber = r.IdNumber, BirthDate = r.BirthDate,
                CreatedAt = now, UpdatedAt = now
            }));

            await db.SaveChangesAsync();

            // ── 5. 薪資同步：找 EffectiveDate <= 今日（Asia/Taipei）的最新薪資紀錄 ──
            // 取得 EffectiveDate 最大（最新有效）的薪資調整紀錄，同步至 User 的 7 個薪資欄位
            // （底薪 + 6 種加給）；無符合不變。
            var today = Clock.Now.Date;
            var latestSalary = salaryEntities
                .Where(s => s.EffectiveDate.Date <= today)
                .OrderByDescending(s => s.EffectiveDate)
                .FirstOrDefault();

            if (latestSalary is not null)
            {
                user.BaseSalary           = latestSalary.BaseSalary;
                user.MealAllowance        = latestSalary.MealAllowance;
                user.PositionAllowance    = latestSalary.PositionAllowance;
                user.DutyAllowance        = latestSalary.DutyAllowance;
                user.OtherAllowance       = latestSalary.OtherAllowance;
                user.AdjustmentDifference = latestSalary.AdjustmentDifference;
                user.OverseasAllowance    = latestSalary.OverseasAllowance;
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
