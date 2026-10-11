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

namespace Jabez.Api.Handlers;

public sealed class UserHandler(AppDbContext db, IUserReadService reader, IEmailService emailService, IBlobStorageService blob, IProjectAccessResolver access)
{
    // GET /me/user → 當前使用者自助查詢自己的完整個人資料（登入即可，不需 users:read）
    // 刻意不套薪資欄位級權限（PayrollFieldAccess）—— 員工看自己的薪資是既有需求，
    // 該權限只管「看別人的」。請勿為了與 GetAllAsync / GetByIdAsync 一致而補上。
    public async Task<IActionResult> GetMineAsync(HttpRequest req)
    {
        var userIdStr = req.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return new UnauthorizedObjectResult(ApiResponse.Fail("Unauthorized.", "Invalid token claims."));

        var user = await reader.GetByIdAsync(userId);
        return user is null
            ? new NotFoundObjectResult(ApiResponse.Fail("User not found."))
            : new OkObjectResult(ApiResponse.Ok(user));
    }

    // GET /api/users/lookup            → 全公司輕量清單（指定審核者下拉用）
    // GET /api/users/lookup?scope=department → 套用部門 scope 過濾（報表頁員工下拉用）
    public async Task<IActionResult> GetLookupAsync(HttpRequest req)
    {
        var scopeMode = req.Query["scope"].ToString();
        if (string.Equals(scopeMode, "department", StringComparison.OrdinalIgnoreCase))
        {
            var scope = await access.ResolveAsync(req.HttpContext.User);
            var scoped = await reader.GetLookupAsync(scope);
            return new OkObjectResult(ApiResponse.Ok(scoped));
        }

        var list = await reader.GetLookupAsync();
        return new OkObjectResult(ApiResponse.Ok(list));
    }

    // GET /api/users — Dapper 讀取（含 JOIN）
    // 欄位級權限：無 payroll:read 者的薪資欄位一律抹為 null（SQL 不動，抹除在 Handler）
    // 篩選：?search= 姓名模糊比對、?departmentId= 部門、?status= 在職狀態、?hasLaborPension= 勞退自提（皆與分頁參數正交）
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        var canSeeSalary = PayrollFieldAccess.CanSeeSalary(req.HttpContext.User);

        var search = req.Query["search"].ToString();
        int? departmentId = int.TryParse(req.Query["departmentId"], out var deptId) ? deptId : null;

        // 在職狀態：白名單正規化，非法值一律忽略
        var statusRaw = req.Query["status"].ToString();
        var status = statusRaw is "active" or "inactive" ? statusRaw : null;

        // 勞退自提：僅接受 true / false，其餘忽略
        bool? hasLaborPension = bool.TryParse(req.Query["hasLaborPension"], out var hp) ? hp : null;

        // 勞退自提率為 payroll:read 欄位級權限保護欄，能以此篩選＝能反推他人自提率
        if (hasLaborPension.HasValue && !canSeeSalary)
            throw AppException.Forbidden("勞退自提篩選需要薪資檢視權限（payroll:read）。");

        // 有分頁參數 → 回傳 PagedResult；無分頁參數 → 回傳平面陣列（供下拉選單用）
        if (req.Query.ContainsKey("page") || req.Query.ContainsKey("pageSize"))
        {
            int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
            int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
            var result = await reader.GetPagedAsync(page, pageSize, search, departmentId, status, hasLaborPension);
            // Items 為 IEnumerable，Select 需 ToList() 具體化，避免延遲到序列化才求值
            if (!canSeeSalary)
                result = result with { Items = result.Items.Select(PayrollFieldAccess.Mask).ToList() };
            return new OkObjectResult(ApiResponse.Ok(result));
        }

        var all = await reader.GetAllAsync(search, departmentId, status, hasLaborPension);
        if (!canSeeSalary)
            all = all.Select(PayrollFieldAccess.Mask).ToList();
        return new OkObjectResult(ApiResponse.Ok(all));
    }

    // GET /api/users/{id}
    public async Task<IActionResult> GetByIdAsync(HttpRequest req, string id)
    {
        if (!Guid.TryParse(id, out var guid))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid user ID format."));

        // 超管帳號不對外暴露
        if (await db.Users.AnyAsync(u => u.Id == guid && u.IsSuperAdmin))
            return new NotFoundObjectResult(ApiResponse.Fail("User not found.", $"No user with id '{id}'."));

        var user = await reader.GetByIdAsync(guid);
        if (user is null)
            return new NotFoundObjectResult(ApiResponse.Fail("User not found.", $"No user with id '{id}'."));

        if (!PayrollFieldAccess.CanSeeSalary(req.HttpContext.User))
            user = PayrollFieldAccess.Mask(user);

        return new OkObjectResult(ApiResponse.Ok(user));
    }

    private const string SignatureContainer        = "signatures";
    private const string AvatarContainer           = "avatars";
    private const string IndigenousProofContainer  = "indigenous-proofs";
    private const string LowIncomeProofContainer   = "low-income-proofs";
    private const string DisabledProofContainer    = "disabled-proofs";
    private const string IdCardContainer           = "id-cards";

    private static readonly string[] AllowedSignatureTypes       = ["image/png", "image/jpeg", "image/gif", "image/webp"];
    private static readonly string[] AllowedAvatarTypes          = ["image/png", "image/jpeg", "image/gif", "image/webp"];
    private static readonly string[] AllowedIndigenousProofTypes = ["image/png", "image/jpeg", "application/pdf"];
    private static readonly string[] AllowedProofTypes           = ["image/png", "image/jpeg", "application/pdf"];

    /// <summary>
    /// 處理檔案上傳的共用邏輯（新增/更新共用）。
    /// 上傳成功後回傳 API 代理路徑（/files/{container}/{blobName}），
    /// 而非直接回傳 Blob URL，避免私有容器 403 及 CORS 問題。
    /// </summary>
    private async Task<string?> HandleFileUploadAsync(
        IFormFileCollection files,
        string              formFieldName,
        string              container,
        string[]            allowedTypes,
        string              badTypeMessage,
        Guid                userId,
        string?             existingUrl)
    {
        var file = files.GetFile(formFieldName);
        if (file is null || file.Length == 0) return existingUrl;

        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            throw AppException.BadRequest(badTypeMessage);

        // 用 magic bytes 偵測實際格式，避免攻擊者偽造 Content-Type 上傳 .exe / .svg(XSS) 等
        string? actualType;
        using (var peek = file.OpenReadStream())
        {
            actualType = await FileSignatureValidator.DetectAsync(peek);
        }
        if (actualType is null || !allowedTypes.Contains(actualType))
            throw AppException.BadRequest(badTypeMessage);

        var ext      = Path.GetExtension(file.FileName);
        var blobName = $"{userId}{ext}";
        var newUrl   = $"files/{container}/{blobName}";

        // 先上傳新檔（若失敗，舊檔保留供後續存取，避免「上傳失敗→使用者頭像消失」的不一致狀態）。
        // 用 actualType（magic bytes 偵測結果）取代客戶端宣告的 ContentType，避免錯誤的 metadata 寫進 Blob。
        using (var stream = file.OpenReadStream())
        {
            await blob.UploadAsync(container, blobName, stream, actualType);
        }

        // 上傳成功後再刪舊檔；同名（同副檔名）會被覆寫，跳過刪除以免誤刪剛上傳的檔案
        if (!string.Equals(existingUrl, newUrl, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await DeleteBlobByUrlAsync(container, existingUrl);
            }
            catch (Exception)
            {
                // 舊檔刪除失敗不阻斷整體流程；新檔已就位，舊檔變為孤兒檔案
                // （比起 throw 造成 user 帶有未存入的 newUrl 而新檔已上傳成功的不一致狀態，
                //   留下一個孤兒檔案是更可承受的後果）
            }
        }

        return newUrl;
    }

    /// <summary>刪除指定 container 中的舊檔（同時支援舊 Blob URL 格式與新 API 代理路徑格式）。</summary>
    private async Task DeleteBlobByUrlAsync(string container, string? existingUrl)
    {
        if (string.IsNullOrEmpty(existingUrl)) return;

        var proxyPrefix = $"files/{container}/";
        var oldBlobName = existingUrl.StartsWith(proxyPrefix, StringComparison.OrdinalIgnoreCase)
            ? existingUrl[proxyPrefix.Length..]
            : blob.ExtractBlobName(existingUrl, container);

        if (oldBlobName is not null)
            await blob.DeleteAsync(container, oldBlobName);
    }

    /// <summary>解析 avatar 位置百分比（0-100），失敗或缺值時回傳 fallback。</summary>
    private static decimal ParseAvatarPosition(Microsoft.Extensions.Primitives.StringValues raw, decimal fallback)
    {
        var text = raw.ToString();
        if (string.IsNullOrEmpty(text)) return fallback;
        if (!decimal.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v))
            return fallback;
        return Math.Clamp(v, 0m, 100m);
    }

    /// <summary>解析 avatar 縮放倍率（1.0-3.0），失敗或缺值時回傳 fallback。</summary>
    private static decimal ParseAvatarScale(Microsoft.Extensions.Primitives.StringValues raw, decimal fallback)
    {
        var text = raw.ToString();
        if (string.IsNullOrEmpty(text)) return fallback;
        if (!decimal.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v))
            return fallback;
        return Math.Clamp(v, 1m, 3m);
    }

    /// <summary>解析勞退自提比例：0-6 之整數，超出範圍拋 400；空值/無法解析視為 null（0%）。</summary>
    private static decimal? ParseLaborPensionRate(Microsoft.Extensions.Primitives.StringValues raw)
    {
        var text = raw.ToString();
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (!decimal.TryParse(text, out var rate)) return null;
        if (rate != Math.Floor(rate) || rate < 0m || rate > 6m)
            throw AppException.BadRequest("勞退自提比例僅能為 0~6 之整數。");
        return rate;
    }

    /// <summary>
    /// 解析自訂上下班時段（賣店等）：<c>hasCustomWorkHours=true</c> 時兩個 "HH:mm" 皆必填、下班晚於上班、
    /// 且須落在 <see cref="ClockProfile"/> 的合法範圍內（超出提醒排程時段會永遠收不到提醒）；
    /// 未勾選一律回 (null, null) ＝ 公司預設。推導規則見 <see cref="ClockProfile.For"/>。
    /// </summary>
    private static (string? Start, string? End) ParseCustomWorkHours(IFormCollection form)
    {
        if (form["hasCustomWorkHours"] != "true") return (null, null);

        if (!ClockProfile.TryParseHHmm(form["customWorkStartTime"], out var start)
            || !ClockProfile.TryParseHHmm(form["customWorkEndTime"], out var end))
            throw AppException.BadRequest("勾選自訂上下班時段時，上班與下班時間皆為必填（HH:mm）。");

        if (end <= start)
            throw AppException.BadRequest("自訂下班時間必須晚於上班時間。");

        if (start < ClockProfile.CustomStartEarliest || start > ClockProfile.CustomStartLatest)
            throw AppException.BadRequest(
                $"自訂上班時間須介於 {ClockProfile.CustomStartEarliest:HH\\:mm}–{ClockProfile.CustomStartLatest:HH\\:mm}（超出此範圍將無法發送上班提醒）。");

        if (end < ClockProfile.CustomEndEarliest || end > ClockProfile.CustomEndLatest)
            throw AppException.BadRequest(
                $"自訂下班時間須介於 {ClockProfile.CustomEndEarliest:HH\\:mm}–{ClockProfile.CustomEndLatest:HH\\:mm}（超出此範圍將無法發送下班提醒）。");

        return (start.ToString("HH\\:mm"), end.ToString("HH\\:mm"));
    }

    private Task<string?> HandleSignatureUploadAsync(IFormFileCollection files, Guid userId, string? existingUrl)
        => HandleFileUploadAsync(files, "signature", SignatureContainer, AllowedSignatureTypes,
            "僅支援 PNG、JPEG、GIF、WebP 圖片格式。", userId, existingUrl);

    private Task<string?> HandleAvatarUploadAsync(IFormFileCollection files, Guid userId, string? existingUrl)
    {
        const long AvatarMaxBytes = 1 * 1024 * 1024; // 1 MB
        var file = files.GetFile("avatar");
        if (file is not null && file.Length > AvatarMaxBytes)
            throw AppException.BadRequest("上傳照片勿超過1MB");

        return HandleFileUploadAsync(files, "avatar", AvatarContainer, AllowedAvatarTypes,
            "頭像僅支援 PNG、JPEG、GIF、WebP 圖片格式。", userId, existingUrl);
    }

    private Task<string?> HandleIndigenousProofUploadAsync(IFormFileCollection files, Guid userId, string? existingUrl)
        => HandleFileUploadAsync(files, "indigenousProof", IndigenousProofContainer, AllowedIndigenousProofTypes,
            "原住民證明文件僅支援 PNG、JPEG 圖片或 PDF 格式。", userId, existingUrl);

    private Task<string?> HandleLowIncomeProofUploadAsync(IFormFileCollection files, Guid userId, string? existingUrl)
    {
        const long ProofMaxBytes = 1 * 1024 * 1024; // 1 MB
        var file = files.GetFile("lowIncomeProof");
        if (file is not null && file.Length > ProofMaxBytes)
            throw AppException.BadRequest("上傳照片勿超過1MB");
        return HandleFileUploadAsync(files, "lowIncomeProof", LowIncomeProofContainer, AllowedProofTypes,
            "低收入戶證明文件僅支援 PNG、JPEG 圖片或 PDF 格式。", userId, existingUrl);
    }

    private Task<string?> HandleDisabledProofUploadAsync(IFormFileCollection files, Guid userId, string? existingUrl)
    {
        const long ProofMaxBytes = 1 * 1024 * 1024; // 1 MB
        var file = files.GetFile("disabledProof");
        if (file is not null && file.Length > ProofMaxBytes)
            throw AppException.BadRequest("上傳照片勿超過1MB");
        return HandleFileUploadAsync(files, "disabledProof", DisabledProofContainer, AllowedProofTypes,
            "身心障礙身份證明文件僅支援 PNG、JPEG 圖片或 PDF 格式。", userId, existingUrl);
    }

    // POST /api/users
    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var form = await req.ReadFormAsync();

        var name     = form["name"].ToString();
        var email    = form["email"].ToString();
        var password = form["password"].ToString();
        var status   = form["status"].ToString();
        var roleIdsRaw = form["roleIds"].ToArray();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            return new BadRequestObjectResult(ApiResponse.Fail("Name and Email are required."));

        if (!string.IsNullOrWhiteSpace(password) && password.Length < 6)
            return new BadRequestObjectResult(ApiResponse.Fail("Password must be at least 6 characters."));

        // 部門必填（Superadmin 例外，但此端點不會建立 Superadmin）
        var createDeptId = int.TryParse(form["departmentId"], out var didCreate) && didCreate > 0 ? didCreate : (int?)null;
        if (createDeptId is null)
            return new BadRequestObjectResult(ApiResponse.Fail("請設定部門。"));
        if (!await db.Departments.AnyAsync(d => d.Id == createDeptId.Value))
            return new BadRequestObjectResult(ApiResponse.Fail("指定的部門不存在。"));

        // 生日（必填，用於產生預設密碼）
        var birthday = DateTime.TryParse(form["birthday"], out var bd) ? bd : (DateTime?)null;
        if (birthday is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Birthday is required."));

        // 密碼：有填則用，否則以生日八位數作為預設密碼
        var effectivePassword = !string.IsNullOrWhiteSpace(password)
            ? password
            : birthday.Value.ToString("yyyyMMdd");

        // 檢查 Email 唯一
        if (await db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
            throw AppException.Conflict($"Email '{email}' is already in use.");

        var (customStart, customEnd) = ParseCustomWorkHours(form);

        var userId = Guid.NewGuid();

        // 角色指派越權檢查：非 Superadmin 只能指派權限為自身子集合的角色（新建使用者無既有角色）
        await EnsureCanAssignRolesAsync(req.HttpContext.User, userId, [], roleIdsRaw);

        // 部門 / 職稱越權檢查（與 UpdateAsync 同一支）：非 Superadmin 不可建立財務體系部門或高職級帳號。
        // 新建帳號無既有部門 / 職稱，傳空 User 使「指定值」一律視為變更。
        if (req.HttpContext.User.FindFirst("is_superadmin")?.Value != "true")
            await EnsureNoPrivilegeEscalationAsync(req.HttpContext.User, form, new User());

        var user = new User
        {
            Id           = userId,
            Name         = name,
            Email        = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(effectivePassword),
            // 不論預設生日密碼或管理員自填密碼，首次登入一律強制改密碼（後端以 JWT claim 強制，見 AppRouter）
            MustChangePassword = true,
            Status       = string.IsNullOrEmpty(status) ? "active" : status,
            DepartmentId = createDeptId,
            JobTitleId   = int.TryParse(form["jobTitleId"], out var jtid) && jtid > 0 ? jtid : null,
            HireDate     = DateTime.TryParse(form["hireDate"], out var hd) ? hd : null,
            ResignDate   = DateTime.TryParse(form["resignDate"], out var rd) ? rd : null,
            BaseSalary    = decimal.TryParse(form["baseSalary"], out var bs) ? bs : null,
            MealAllowance = decimal.TryParse(form["mealAllowance"], out var ma) ? ma : null,
            OvertimePay   = decimal.TryParse(form["overtimePay"], out var op) ? op : null,
            SendPaySlip   = form["sendPaySlip"] == "true",
            CompensatoryOpeningHours = decimal.TryParse(form["compensatoryOpeningHours"], out var coh) ? coh : 0m,
            IsShiftWorker = form["isShiftWorker"] == "true",
            CustomWorkStartTime = customStart,
            CustomWorkEndTime   = customEnd,
            AgentUserId   = Guid.TryParse(form["agentUserId"], out var aid) && aid != Guid.Empty ? aid : null,
            Birthday     = birthday,
            IsIndigenous = form["isIndigenous"] == "true",
            IsLowIncome  = form["isLowIncome"] == "true",
            IsDisabled   = form["isDisabled"] == "true",
            HealthInsuranceOverride = decimal.TryParse(form["healthInsuranceOverride"], out var hio) ? hio : null,
            LaborInsuranceOverride  = decimal.TryParse(form["laborInsuranceOverride"],  out var lio) ? lio : null,
            LaborPensionSelfContributionRate = ParseLaborPensionRate(form["laborPensionSelfContributionRate"]),
            OtherAllowance       = decimal.TryParse(form["otherAllowance"],       out var oa)  ? oa  : null,
            AdjustmentDifference = decimal.TryParse(form["adjustmentDifference"], out var ad)  ? ad  : null,
            AvatarPositionX = ParseAvatarPosition(form["avatarPositionX"], 50m),
            AvatarPositionY = ParseAvatarPosition(form["avatarPositionY"], 50m),
            AvatarScale     = ParseAvatarScale(form["avatarScale"], 1m),
            CreatedAt    = Clock.Now,
            UpdatedAt    = Clock.Now,
        };

        // 薪資欄位級權限：讀寫同一道 gate —— 看不到就不該寫得進去。
        // 無 payroll:read 者的前端不會送這些 key，這裡把值一律清掉（不回 403，其他欄位照常建立）。
        if (!PayrollFieldAccess.CanSeeSalary(req.HttpContext.User))
        {
            user.BaseSalary                       = null;
            user.MealAllowance                    = null;
            user.OvertimePay                      = null;
            user.OtherAllowance                   = null;
            user.AdjustmentDifference             = null;
            user.HealthInsuranceOverride          = null;
            user.LaborInsuranceOverride           = null;
            user.LaborPensionSelfContributionRate = null;
        }

        // 處理檔案上傳：簽名檔、頭像、原住民證明、低收入證明、身心障礙證明
        user.SignatureUrl = await HandleSignatureUploadAsync(form.Files, userId, null);
        user.Avatar       = await HandleAvatarUploadAsync(form.Files, userId, null);

        if (user.IsIndigenous)
        {
            user.IndigenousProofUrl = await HandleIndigenousProofUploadAsync(form.Files, userId, null);
            if (user.IndigenousProofUrl is null)
                throw AppException.BadRequest("勾選原住民身分時必須上傳證明文件。");
        }

        if (user.IsLowIncome)
        {
            user.LowIncomeProofUrl = await HandleLowIncomeProofUploadAsync(form.Files, userId, null);
            if (user.LowIncomeProofUrl is null)
                throw AppException.BadRequest("勾選低收入戶時必須上傳證明文件。");
        }

        if (user.IsDisabled)
        {
            user.DisabledProofUrl = await HandleDisabledProofUploadAsync(form.Files, userId, null);
            if (user.DisabledProofUrl is null)
                throw AppException.BadRequest("勾選身心障礙身份時必須上傳證明文件。");
        }

        db.Users.Add(user);

        // 處理 Roles
        foreach (var roleId in roleIdsRaw)
            if (!string.IsNullOrEmpty(roleId))
                db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(user.Id);
        if (dto is not null && !PayrollFieldAccess.CanSeeSalary(req.HttpContext.User))
            dto = PayrollFieldAccess.Mask(dto);
        return new ObjectResult(ApiResponse.Ok(dto, "User created.")) { StatusCode = 201 };
    }

    // PUT /api/users/{id}
    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        if (!Guid.TryParse(id, out var guid))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid user ID format."));

        var form = await req.ReadFormAsync();

        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == guid)
            ?? throw AppException.NotFound("User");

        if (user.IsSuperAdmin)
            throw AppException.Forbidden("Cannot modify the system super admin account.");

        // 角色指派越權檢查：放在任何 Blob 上傳 / 欄位寫入之前，被擋時不留副作用。
        // 不可改自己的角色；非 Superadmin 只能「新增」權限為自身子集合的角色。
        if (form.ContainsKey("roleIds"))
            await EnsureCanAssignRolesAsync(req.HttpContext.User, guid,
                user.UserRoles.Select(ur => ur.RoleId).ToArray(), form["roleIds"].ToArray());

        var nameVal     = form["name"].ToString();
        var emailVal    = form["email"].ToString();
        var statusVal   = form["status"].ToString();
        var passwordVal = form["password"].ToString();

        var principal    = req.HttpContext.User;
        var isSuperAdmin = principal.FindFirst("is_superadmin")?.Value == "true";
        var isSelf       = IsOperator(req, guid);

        // 提權防線（皆放在任何 Blob 上傳 / 欄位寫入之前，被擋時不留副作用）：
        // 1. 非 Superadmin 不得修改自己的部門 / 職稱 / 狀態 / 代理人 / 補休期初 / 薪資 / 簽名檔
        // 2. 調動至財務體系部門、或調高職級，限 Superadmin
        // 3. 設定他人密碼：目標帳號的權限須為操作者權限的子集合
        if (!isSuperAdmin)
        {
            if (isSelf)
                EnsureSelfEditAllowed(form, user, PayrollFieldAccess.CanSeeSalary(principal));
            await EnsureNoPrivilegeEscalationAsync(principal, form, user);
            // 改他人密碼或 email（email 可經忘記密碼流程接管帳號）：目標權限須為操作者子集合。
            // 本人改自己的 email 不受限（self-edit 禁改清單不含 email）。
            var emailChanged = !string.IsNullOrEmpty(emailVal)
                               && !string.Equals(emailVal, user.Email, StringComparison.OrdinalIgnoreCase);
            if (!isSelf && (!string.IsNullOrEmpty(passwordVal) || emailChanged))
                await EnsureTargetIsSubordinateAsync(principal, user);
        }

        var auditBefore = AuditSnapshot.Capture(user);

        if (!string.IsNullOrEmpty(nameVal))     user.Name   = nameVal;
        if (!string.IsNullOrEmpty(emailVal))    user.Email  = emailVal;

        // 帳號停用 / 管理員設定密碼 / 角色變更：存檔後撤銷該使用者全部 Refresh Token
        var revokeTokens = false;
        // 部門 / 職稱變更：只換安全戳記（舊 access token 立即 401），不撤 refresh token ——
        // 對方 refresh 即可無縫換到帶新 department_id / job_title_level 的 token
        var bumpStamp = false;

        if (!string.IsNullOrEmpty(statusVal))
        {
            if (statusVal == "inactive" && user.Status != "inactive") revokeTokens = true;
            user.Status = statusVal;
        }
        if (!string.IsNullOrEmpty(passwordVal))
        {
            if (passwordVal.Length < 6)
                return new BadRequestObjectResult(ApiResponse.Fail("Password must be at least 6 characters."));
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordVal);
            // 管理員設定的密碼＝管理員知道的密碼 → 對方下次登入必須自己改掉。
            // 管理員改自己的密碼不算（他自己設的、自己知道）。
            if (!IsOperator(req, guid))
                user.MustChangePassword = true;
            revokeTokens = true;
        }

        if (form.ContainsKey("departmentId"))
        {
            var did = int.TryParse(form["departmentId"], out var didV) && didV > 0 ? didV : (int?)null;
            if (did is null)
                return new BadRequestObjectResult(ApiResponse.Fail("請設定部門。"));
            if (!await db.Departments.AnyAsync(d => d.Id == did.Value))
                return new BadRequestObjectResult(ApiResponse.Fail("指定的部門不存在。"));
            if (did != user.DepartmentId) bumpStamp = true;
            user.DepartmentId = did;
        }
        if (form.ContainsKey("jobTitleId"))
        {
            var newTitleId = int.TryParse(form["jobTitleId"], out var jtid) && jtid > 0 ? jtid : (int?)null;
            if (newTitleId != user.JobTitleId) bumpStamp = true;
            user.JobTitleId = newTitleId;
        }
        if (form.ContainsKey("hireDate"))
            user.HireDate = DateTime.TryParse(form["hireDate"], out var hd) ? hd : null;
        if (form.ContainsKey("resignDate"))
            user.ResignDate = DateTime.TryParse(form["resignDate"], out var rd) ? rd : null;
        if (form.ContainsKey("sendPaySlip"))
            user.SendPaySlip = form["sendPaySlip"] == "true";
        if (form.ContainsKey("compensatoryOpeningHours"))
            user.CompensatoryOpeningHours = decimal.TryParse(form["compensatoryOpeningHours"], out var coh) ? coh : 0m;
        if (form.ContainsKey("isShiftWorker"))
            user.IsShiftWorker = form["isShiftWorker"] == "true";
        if (form.ContainsKey("hasCustomWorkHours"))
            (user.CustomWorkStartTime, user.CustomWorkEndTime) = ParseCustomWorkHours(form);
        if (form.ContainsKey("agentUserId"))
            user.AgentUserId = Guid.TryParse(form["agentUserId"], out var aid) && aid != Guid.Empty ? aid : null;
        if (form.ContainsKey("birthday"))
            user.Birthday = DateTime.TryParse(form["birthday"], out var bd) ? bd : null;
        if (form.ContainsKey("isIndigenous"))
            user.IsIndigenous = form["isIndigenous"] == "true";
        if (form.ContainsKey("isLowIncome"))
            user.IsLowIncome = form["isLowIncome"] == "true";
        if (form.ContainsKey("isDisabled"))
            user.IsDisabled = form["isDisabled"] == "true";
        // 薪資欄位級權限：11 個薪資 / 勞健保欄位集中在此，無 payroll:read 者整段不受理。
        // 讀寫同一道 gate —— 看不到就不該改得動。不回 403：其他欄位（部門、離職日…）必須照常存檔。
        if (PayrollFieldAccess.CanSeeSalary(req.HttpContext.User))
        {
            if (form.ContainsKey("baseSalary"))
                user.BaseSalary = decimal.TryParse(form["baseSalary"], out var bs) ? bs : null;
            if (form.ContainsKey("mealAllowance"))
                user.MealAllowance = decimal.TryParse(form["mealAllowance"], out var ma) ? ma : null;
            if (form.ContainsKey("overtimePay"))
                user.OvertimePay = decimal.TryParse(form["overtimePay"], out var op) ? op : null;
            if (form.ContainsKey("healthInsuranceOverride"))
                user.HealthInsuranceOverride = decimal.TryParse(form["healthInsuranceOverride"], out var hio) ? hio : null;
            if (form.ContainsKey("laborInsuranceOverride"))
                user.LaborInsuranceOverride = decimal.TryParse(form["laborInsuranceOverride"], out var lio) ? lio : null;
            if (form.ContainsKey("laborPensionSelfContributionRate"))
                user.LaborPensionSelfContributionRate = ParseLaborPensionRate(form["laborPensionSelfContributionRate"]);
            if (form.ContainsKey("otherAllowance"))
                user.OtherAllowance = decimal.TryParse(form["otherAllowance"], out var oa) ? oa : null;
            if (form.ContainsKey("adjustmentDifference"))
                user.AdjustmentDifference = decimal.TryParse(form["adjustmentDifference"], out var ad) ? ad : null;
        }

        // 處理簽名檔：removeSignature=true 表示刪除
        if (form["removeSignature"] == "true")
        {
            await DeleteBlobByUrlAsync(SignatureContainer, user.SignatureUrl);
            user.SignatureUrl = null;
        }
        else
        {
            user.SignatureUrl = await HandleSignatureUploadAsync(form.Files, guid, user.SignatureUrl);
        }

        // 處理頭像：removeAvatar=true 表示刪除
        if (form["removeAvatar"] == "true")
        {
            await DeleteBlobByUrlAsync(AvatarContainer, user.Avatar);
            user.Avatar = null;
            // 沒頭像就沒位置概念，重置為預設值
            user.AvatarPositionX = 50m;
            user.AvatarPositionY = 50m;
            user.AvatarScale     = 1m;
        }
        else
        {
            user.Avatar = await HandleAvatarUploadAsync(form.Files, guid, user.Avatar);
        }

        // 頭像位置 / 縮放（更換頭像時不重置，保留使用者上次調整）
        if (form.ContainsKey("avatarPositionX"))
            user.AvatarPositionX = ParseAvatarPosition(form["avatarPositionX"], user.AvatarPositionX);
        if (form.ContainsKey("avatarPositionY"))
            user.AvatarPositionY = ParseAvatarPosition(form["avatarPositionY"], user.AvatarPositionY);
        if (form.ContainsKey("avatarScale"))
            user.AvatarScale = ParseAvatarScale(form["avatarScale"], user.AvatarScale);

        // 處理原住民證明文件：
        // 1. 若 IsIndigenous 由 true → false：自動刪除證明檔
        // 2. 若 removeIndigenousProof=true：刪除
        // 3. 否則依上傳檔案覆寫
        // 4. 最後檢查：IsIndigenous=true 時必須有證明檔
        if (!user.IsIndigenous)
        {
            await DeleteBlobByUrlAsync(IndigenousProofContainer, user.IndigenousProofUrl);
            user.IndigenousProofUrl = null;
        }
        else if (form["removeIndigenousProof"] == "true")
        {
            await DeleteBlobByUrlAsync(IndigenousProofContainer, user.IndigenousProofUrl);
            user.IndigenousProofUrl = null;
        }
        else
        {
            user.IndigenousProofUrl = await HandleIndigenousProofUploadAsync(form.Files, guid, user.IndigenousProofUrl);
        }

        if (user.IsIndigenous && string.IsNullOrEmpty(user.IndigenousProofUrl))
            throw AppException.BadRequest("勾選原住民身分時必須上傳證明文件。");

        // 處理低收入戶證明：
        // 1. 若 IsLowIncome 由 true → false：自動刪除證明檔
        // 2. 若 removeLowIncomeProof=true：刪除
        // 3. 否則依上傳檔案覆寫
        // 4. 最後檢查：IsLowIncome=true 時必須有證明檔
        if (!user.IsLowIncome)
        {
            await DeleteBlobByUrlAsync(LowIncomeProofContainer, user.LowIncomeProofUrl);
            user.LowIncomeProofUrl = null;
        }
        else if (form["removeLowIncomeProof"] == "true")
        {
            await DeleteBlobByUrlAsync(LowIncomeProofContainer, user.LowIncomeProofUrl);
            user.LowIncomeProofUrl = null;
        }
        else
        {
            user.LowIncomeProofUrl = await HandleLowIncomeProofUploadAsync(form.Files, guid, user.LowIncomeProofUrl);
        }

        if (user.IsLowIncome && string.IsNullOrEmpty(user.LowIncomeProofUrl))
            throw AppException.BadRequest("勾選低收入戶時必須上傳證明文件。");

        // 處理身心障礙身份證明（同上模式）
        if (!user.IsDisabled)
        {
            await DeleteBlobByUrlAsync(DisabledProofContainer, user.DisabledProofUrl);
            user.DisabledProofUrl = null;
        }
        else if (form["removeDisabledProof"] == "true")
        {
            await DeleteBlobByUrlAsync(DisabledProofContainer, user.DisabledProofUrl);
            user.DisabledProofUrl = null;
        }
        else
        {
            user.DisabledProofUrl = await HandleDisabledProofUploadAsync(form.Files, guid, user.DisabledProofUrl);
        }

        if (user.IsDisabled && string.IsNullOrEmpty(user.DisabledProofUrl))
            throw AppException.BadRequest("勾選身心障礙身份時必須上傳證明文件。");

        user.UpdatedAt = Clock.Now;

        // 更新 Roles
        if (form.ContainsKey("roleIds"))
        {
            var roleIds = form["roleIds"].ToArray();
            var currentRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToArray();

            var requestedSet = roleIds.Where(r => !string.IsNullOrEmpty(r)).ToHashSet();
            if (!requestedSet.SetEquals(currentRoleIds))
                revokeTokens = true;   // 角色變更：舊 token 的 permissions 快照作廢，逼對方重新登入取新權限

            db.UserRoles.RemoveRange(user.UserRoles);
            foreach (var roleId in requestedSet)
                db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        await db.SaveChangesAsync();

        if (revokeTokens)
            await RefreshTokenRevoker.RevokeAllAsync(db, user.Id);
        else if (bumpStamp)
            await RefreshTokenRevoker.BumpSecurityStampAsync(db, user.Id);

        // 稽核：存檔成功後才落紀錄（失敗的請求不留）。設密碼獨立一筆動作，方便依動作查詢。
        var changes = AuditSnapshot.Diff(auditBefore, AuditSnapshot.Capture(user));
        if (!string.IsNullOrEmpty(passwordVal))
            await WriteAuditAsync(req, guid, "set_password", null);
        if (changes.Length > 0)
            await WriteAuditAsync(req, guid, "update", changes);

        var dto = await reader.GetByIdAsync(user.Id);
        // 更新回應也要抹除：無權者若拿回既有薪資值，等於繞過 GetByIdAsync 的遮蔽
        if (dto is not null && !PayrollFieldAccess.CanSeeSalary(req.HttpContext.User))
            dto = PayrollFieldAccess.Mask(dto);
        return new OkObjectResult(ApiResponse.Ok(dto, "User updated."));
    }

    // DELETE /api/users/{id}
    public async Task<IActionResult> DeleteAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid user ID format."));

        var user = await db.Users.FindAsync(guid)
            ?? throw AppException.NotFound("User");

        if (user.IsSuperAdmin)
            throw AppException.Forbidden("Cannot delete the system super admin account.");

        // 先記下需清理的 Blob URL（DB 刪除後就拿不到了）；
        // 各類 PII 證明文件不清理會造成已離職員工的個資長期殘留在 Storage
        var avatarUrl         = user.Avatar;
        var signatureUrl      = user.SignatureUrl;
        var proofUrl          = user.IndigenousProofUrl;
        var lowIncomeUrl      = user.LowIncomeProofUrl;
        var disabledUrl       = user.DisabledProofUrl;

        // EmployeeProfile 身分證正反面需先從 DB 讀取，因 EF Cascade 會連帶刪除
        var profile = await db.EmployeeProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == guid);
        var idCardFrontUrl = profile?.IdCardFrontUrl;
        var idCardBackUrl  = profile?.IdCardBackUrl;

        // ── 硬刪除前清洗所有對 Users 的 NO_ACTION 外鍵（否則 DB 會擋住刪除）─────────────
        // CASCADE 外鍵（EmployeeProfile + 9 子表 / AttendanceRecords / RefreshTokens / UserRoles /
        //   PayrollAdjustments / SalaryAdjustmentRecords）由 DB 自動連帶刪除；
        // SET NULL 外鍵（各申請單 SubmittedById / EmployeeId）由 DB 自動設 NULL；
        // 以下手動處理剩餘的 NO_ACTION 外鍵，全部包在交易內確保原子性。
        // DbContext 啟用 EnableRetryOnFailure，直接 BeginTransactionAsync 會被阻擋，須透過 strategy 執行。
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();

            // (1) 主體即該員的列 → 直接刪除（含不可為 NULL 的 ReviewerId / 該員自己的提醒紀錄）
            await db.RequestDesignatedReviewers.Where(r => r.ReviewerId == guid).ExecuteDeleteAsync();
            await db.ApprovalStepExceptions.Where(e => e.UserId == guid).ExecuteDeleteAsync();
            await db.EscalationOverrides.Where(o => o.ReviewerId == guid).ExecuteDeleteAsync();
            await db.ApprovalRecords.Where(r => r.ReviewedById == guid).ExecuteDeleteAsync();
            await db.TravelRequestParticipants.Where(p => p.UserId == guid).ExecuteDeleteAsync();
            await db.AttendanceReminderLogs.Where(l => l.UserId == guid).ExecuteDeleteAsync();
            await db.PaymentReminderLogs.Where(l => l.FinanceUserId == guid).ExecuteDeleteAsync();

            // (2) 屬於其他單據、僅將該員列為審核者 / 撥款者 / 代理人 / 觸發者 → 設 NULL，保留單據本體
            await db.ApprovalRecords.Where(r => r.OnBehalfOfUserId == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.OnBehalfOfUserId, (Guid?)null));
            await db.EscalationOverrides.Where(o => o.OnBehalfOfUserId == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.OnBehalfOfUserId, (Guid?)null));
            await db.AttendanceReminderLogs.Where(l => l.TriggeredByUserId == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.TriggeredByUserId, (Guid?)null));
            await db.PaymentReminderLogs.Where(l => l.TriggeredByUserId == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.TriggeredByUserId, (Guid?)null));
            await db.Users.Where(u => u.AgentUserId == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.AgentUserId, (Guid?)null));

            await db.PaymentRequests.Where(x => x.ReviewedById == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReviewedById, (Guid?)null));
            await db.LeaveRequests.Where(x => x.ReviewedById == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReviewedById, (Guid?)null));
            await db.LeaveRequests.Where(x => x.AgentUserId == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.AgentUserId, (Guid?)null));
            await db.LeaveRevocations.Where(x => x.ReviewedById == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReviewedById, (Guid?)null));
            await db.LeaveRevocations.Where(x => x.EmployeeId == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EmployeeId, (Guid?)null));
            // 改班申請：兩個 FK 都是 NoAction，不清洗會刪不掉使用者
            await db.ShiftChangeRequests.Where(x => x.ReviewedById == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReviewedById, (Guid?)null));
            await db.ShiftChangeRequests.Where(x => x.EmployeeId == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EmployeeId, (Guid?)null));
            await db.OvertimeRequests.Where(x => x.ReviewedById == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReviewedById, (Guid?)null));
            await db.TravelPaymentRequests.Where(x => x.ReviewedById == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReviewedById, (Guid?)null));

            await db.AdvanceRequests.Where(x => x.ReviewedById == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReviewedById, (Guid?)null));
            await db.AdvanceRequests.Where(x => x.ClosedById == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ClosedById, (Guid?)null));
            await db.AdvanceRequests.Where(x => x.RefundedByUserId == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RefundedByUserId, (Guid?)null));

            await db.TravelRequests.Where(x => x.ReviewedById == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReviewedById, (Guid?)null));
            await db.TravelRequests.Where(x => x.ClosedById == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ClosedById, (Guid?)null));
            await db.TravelRequests.Where(x => x.RefundedByUserId == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RefundedByUserId, (Guid?)null));

            await db.WriteOffRecords.Where(x => x.SubmittedById == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SubmittedById, (Guid?)null));
            await db.WriteOffRecords.Where(x => x.ReviewedById == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReviewedById, (Guid?)null));
            await db.TravelWriteOffRecords.Where(x => x.SubmittedById == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SubmittedById, (Guid?)null));
            await db.TravelWriteOffRecords.Where(x => x.ReviewedById == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReviewedById, (Guid?)null));

            await db.PaymentRequestInstallments.Where(x => x.PaidByUserId == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.PaidByUserId, (Guid?)null));
            await db.AdvanceRequestInstallments.Where(x => x.PaidByUserId == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.PaidByUserId, (Guid?)null));
            await db.TravelRequestInstallments.Where(x => x.PaidByUserId == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.PaidByUserId, (Guid?)null));
            await db.TravelPaymentRequestInstallments.Where(x => x.PaidByUserId == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.PaidByUserId, (Guid?)null));
            await db.WriteOffInstallments.Where(x => x.PaidByUserId == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.PaidByUserId, (Guid?)null));
            await db.WriteOffItems.Where(x => x.CheckPaidById == guid)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.CheckPaidById, (Guid?)null));

            db.Users.Remove(user);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        });

        // DB 刪除完成後再清理 Blob：失敗不影響使用者刪除結果（孤兒檔案下次手動清理即可）
        await TryDeleteBlobAsync(AvatarContainer, avatarUrl);
        await TryDeleteBlobAsync(SignatureContainer, signatureUrl);
        await TryDeleteBlobAsync(IndigenousProofContainer, proofUrl);
        await TryDeleteBlobAsync(LowIncomeProofContainer, lowIncomeUrl);
        await TryDeleteBlobAsync(DisabledProofContainer, disabledUrl);
        await TryDeleteBlobAsync(IdCardContainer, idCardFrontUrl);
        await TryDeleteBlobAsync(IdCardContainer, idCardBackUrl);

        return new OkObjectResult(ApiResponse.Ok($"User '{id}' deleted."));
    }

    /// <summary>
    /// 非 Superadmin 修改「自己」時，下列欄位的**實質變更**一律擋下（值與現況相同＝前端整張表單回送，放行）：
    /// 部門 / 職稱 / 狀態 / 職務代理人 / 補休期初時數 / 薪資欄 / 簽名檔。
    /// 否則持 users:write 者可自行調到財務部、調高職級、改自己底薪，或換掉簽名檔冒簽。
    /// </summary>
    private static void EnsureSelfEditAllowed(IFormCollection form, User user, bool canSeeSalary)
    {
        var blocked = new List<string>();

        if (form.ContainsKey("departmentId"))
        {
            var did = int.TryParse(form["departmentId"], out var v) && v > 0 ? v : (int?)null;
            if (did != user.DepartmentId) blocked.Add("部門");
        }
        if (form.ContainsKey("jobTitleId"))
        {
            var jt = int.TryParse(form["jobTitleId"], out var v) && v > 0 ? v : (int?)null;
            if (jt != user.JobTitleId) blocked.Add("職稱");
        }
        var status = form["status"].ToString();
        if (!string.IsNullOrEmpty(status) && status != user.Status) blocked.Add("在職狀態");

        if (form.ContainsKey("agentUserId"))
        {
            var ag = Guid.TryParse(form["agentUserId"], out var v) && v != Guid.Empty ? v : (Guid?)null;
            if (ag != user.AgentUserId) blocked.Add("職務代理人");
        }
        if (form.ContainsKey("compensatoryOpeningHours"))
        {
            var coh = decimal.TryParse(form["compensatoryOpeningHours"], out var v) ? v : 0m;
            if (coh != user.CompensatoryOpeningHours) blocked.Add("補休期初時數");
        }

        if (canSeeSalary)
        {
            static bool Changed(IFormCollection f, string key, decimal? current)
                => f.ContainsKey(key) && (decimal.TryParse(f[key], out var v) ? v : (decimal?)null) != current;

            if (Changed(form, "baseSalary",               user.BaseSalary)
             || Changed(form, "mealAllowance",            user.MealAllowance)
             || Changed(form, "overtimePay",              user.OvertimePay)
             || Changed(form, "healthInsuranceOverride",  user.HealthInsuranceOverride)
             || Changed(form, "laborInsuranceOverride",   user.LaborInsuranceOverride)
             || Changed(form, "otherAllowance",           user.OtherAllowance)
             || Changed(form, "adjustmentDifference",     user.AdjustmentDifference)
             || (form.ContainsKey("laborPensionSelfContributionRate")
                 && ParseLaborPensionRate(form["laborPensionSelfContributionRate"]) != user.LaborPensionSelfContributionRate))
                blocked.Add("薪資");
        }

        if (form["removeSignature"] == "true" || form.Files.Any(f => f.Name == "signature"))
            blocked.Add("簽名檔");

        if (blocked.Count > 0)
            throw AppException.Forbidden($"不可修改自己的{string.Join("、", blocked)}。");
    }

    /// <summary>
    /// 調動 / 升遷越權防線（非 Superadmin 操作者才會進來；只檢查「實質變更」的欄位）：
    /// 新部門屬財務體系（DepartmentCodes.FinancialAndAbove）、或新職稱 Level ≤ 1（總監級）
    /// 或比操作者自身職級更高（Level 數字更小），一律限 Superadmin。
    /// 操作者本身沒有職稱時視為最低職級，任何職稱變更都會被擋（寧嚴勿鬆）。
    /// </summary>
    private async Task EnsureNoPrivilegeEscalationAsync(System.Security.Claims.ClaimsPrincipal operatorPrincipal, IFormCollection form, User target)
    {
        if (form.ContainsKey("departmentId")
            && int.TryParse(form["departmentId"], out var newDept) && newDept > 0 && newDept != target.DepartmentId)
        {
            var code = await db.Departments.AsNoTracking()
                .Where(d => d.Id == newDept).Select(d => d.Code).FirstOrDefaultAsync();
            if (code is not null && DepartmentCodes.FinancialAndAbove.Contains(code))
                throw AppException.Forbidden("調動至財務體系部門限 Superadmin 操作。");
        }

        if (form.ContainsKey("jobTitleId")
            && int.TryParse(form["jobTitleId"], out var newTitle) && newTitle > 0 && newTitle != target.JobTitleId)
        {
            var newLevel = await db.JobTitles.AsNoTracking()
                .Where(j => j.Id == newTitle).Select(j => (int?)j.Level).FirstOrDefaultAsync();
            if (newLevel is int nl)
            {
                var operatorLevel = int.MaxValue;
                if (Guid.TryParse(operatorPrincipal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var opId))
                    operatorLevel = await db.Users.AsNoTracking()
                        .Where(u => u.Id == opId && u.JobTitle != null)
                        .Select(u => (int?)u.JobTitle!.Level).FirstOrDefaultAsync() ?? int.MaxValue;

                if (nl <= 1 || nl < operatorLevel)
                    throw AppException.Forbidden("調高職級（總監級或高於自身職級）限 Superadmin 操作。");
            }
        }
    }

    /// <summary>
    /// 目標帳號的有效權限（其所有角色的權限聯集）必須是操作者權限的子集合，否則 Forbidden。
    /// 防止低權管理員改高權帳號的密碼後以其身分登入（帳號接管＝提權）。Superadmin 操作者由呼叫端略過。
    /// </summary>
    private async Task EnsureTargetIsSubordinateAsync(System.Security.Claims.ClaimsPrincipal operatorPrincipal, User target)
    {
        var targetPerms = await db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == target.Id)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
            .Distinct()
            .ToListAsync();

        var operatorPerms = operatorPrincipal.FindAll("permissions").Select(c => c.Value).ToHashSet();
        var missing = targetPerms.Where(code => !operatorPerms.Contains(code)).ToArray();
        if (missing.Length > 0)
            throw AppException.Forbidden("不可對權限高於自己的帳號執行此操作（目標帳號擁有您所沒有的權限）。");
    }

    /// <summary>寫一筆帳號異動稽核（獨立 SaveChanges；操作者取自 JWT sub）。</summary>
    private async Task WriteAuditAsync(HttpRequest req, Guid targetUserId, string action, string? changes)
    {
        if (!Guid.TryParse(req.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var operatorId))
            return;
        db.UserAuditLogs.Add(new UserAuditLog
        {
            Id             = Guid.NewGuid(),
            TargetUserId   = targetUserId,
            OperatorUserId = operatorId,
            Action         = action,
            Changes        = changes is { Length: > 2000 } ? changes[..2000] : changes,
            CreatedAt      = Clock.Now,
        });
        await db.SaveChangesAsync();
    }

    /// <summary>稽核用的變更前後快照。薪資欄只比對「有無變更」、不落值；簽名檔只記「已變更」。</summary>
    private sealed record AuditSnapshot(
        string Name, string Email, string Status, int? DepartmentId, int? JobTitleId, Guid? AgentUserId,
        decimal CompensatoryOpeningHours, string Salary, string? SignatureUrl, string Roles)
    {
        public static AuditSnapshot Capture(User u) => new(
            u.Name, u.Email, u.Status, u.DepartmentId, u.JobTitleId, u.AgentUserId, u.CompensatoryOpeningHours,
            string.Join("|", u.BaseSalary, u.MealAllowance, u.OvertimePay, u.HealthInsuranceOverride, u.LaborInsuranceOverride,
                        u.LaborPensionSelfContributionRate, u.OtherAllowance, u.AdjustmentDifference),
            u.SignatureUrl,
            string.Join(",", u.UserRoles.Select(r => r.RoleId).OrderBy(x => x)));

        public static string Diff(AuditSnapshot a, AuditSnapshot b)
        {
            var parts = new List<string>();
            void D<T>(string field, T x, T y) { if (!EqualityComparer<T>.Default.Equals(x, y)) parts.Add($"{field}: {x} → {y}"); }
            D("name", a.Name, b.Name);
            D("email", a.Email, b.Email);
            D("status", a.Status, b.Status);
            D("departmentId", a.DepartmentId, b.DepartmentId);
            D("jobTitleId", a.JobTitleId, b.JobTitleId);
            D("agentUserId", a.AgentUserId, b.AgentUserId);
            D("compensatoryOpeningHours", a.CompensatoryOpeningHours, b.CompensatoryOpeningHours);
            if (a.Salary != b.Salary) parts.Add("salary: changed");
            if (a.SignatureUrl != b.SignatureUrl) parts.Add("signature: changed");
            D("roleIds", a.Roles, b.Roles);
            return string.Join("; ", parts);
        }
    }

    private static bool IsOperator(HttpRequest req, Guid targetUserId)
        => Guid.TryParse(req.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var me) && me == targetUserId;

    /// <summary>
    /// 角色指派的越權檢查（防止「有 users:write 就能把自己或同夥提權」）：
    /// <list type="number">
    ///   <item>指定的角色必須存在（否則原本會在寫入時撞 FK 回 500）。</item>
    ///   <item>Superadmin 不受限。</item>
    ///   <item>非 Superadmin **不可改自己的角色**（新增或移除皆不行）。</item>
    ///   <item>非 Superadmin 新增的每個角色，其權限必須是操作者自身權限的子集合
    ///         （比對的是 JWT 的 permissions claim，與 RequirePermission 同一份快照）。
    ///         目標使用者「原本就有」的角色不重驗，避免低權管理員改一下別欄位就被舊角色卡住；移除角色屬降權，不檢查。</item>
    /// </list>
    /// </summary>
    private async Task EnsureCanAssignRolesAsync(
        System.Security.Claims.ClaimsPrincipal operatorPrincipal,
        Guid targetUserId,
        IReadOnlyCollection<string> currentRoleIds,
        IReadOnlyCollection<string> requestedRoleIds)
    {
        var requested = requestedRoleIds.Where(r => !string.IsNullOrEmpty(r)).Distinct().ToArray();
        var current   = currentRoleIds.ToHashSet();
        var added     = requested.Where(r => !current.Contains(r)).ToArray();
        var removed   = current.Where(r => !requested.Contains(r)).ToArray();

        var roles = added.Length == 0
            ? []
            : await db.Roles.AsNoTracking()
                .Where(r => added.Contains(r.Id))
                .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .ToListAsync();
        if (roles.Count != added.Length)
            throw AppException.BadRequest("指定的角色不存在。");

        if (operatorPrincipal.FindFirst("is_superadmin")?.Value == "true")
            return;

        var operatorIdStr = operatorPrincipal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (Guid.TryParse(operatorIdStr, out var operatorId) && operatorId == targetUserId
            && (added.Length > 0 || removed.Length > 0))
            throw AppException.Forbidden("不可修改自己的角色。");

        var operatorPerms = operatorPrincipal.FindAll("permissions").Select(c => c.Value).ToHashSet();
        foreach (var role in roles)
        {
            var missing = role.RolePermissions
                .Select(rp => rp.Permission.Code)
                .Where(code => !operatorPerms.Contains(code))
                .ToArray();
            if (missing.Length > 0)
                throw AppException.Forbidden($"不可指派超出自身權限的角色「{role.Name}」（缺少：{string.Join("、", missing)}）。");
        }
    }

    /// <summary>嘗試刪除 Blob；任何錯誤都吞掉以免阻斷主流程。</summary>
    private async Task TryDeleteBlobAsync(string container, string? url)
    {
        if (string.IsNullOrEmpty(url)) return;
        try { await DeleteBlobByUrlAsync(container, url); }
        catch (Exception) { /* 孤兒檔案：可接受 */ }
    }

    /// <summary>
    /// 寄出員工帳號通知信。
    /// 預設密碼為員工生日八碼（yyyyMMdd），員工首次登入後須立即修改密碼。
    /// 若員工尚未設定生日，則無法寄出通知信。
    /// </summary>
    public async Task<IActionResult> SendCredentialsAsync(HttpRequest req, string id)
    {
        if (!Guid.TryParse(id, out var guid))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid user ID format."));

        var user = await db.Users.FindAsync(guid)
            ?? throw AppException.NotFound("User");

        if (user.IsSuperAdmin)
            throw AppException.Forbidden("Cannot send credentials for the system super admin account.");

        // 重設他人密碼＝帳號接管風險：非 Superadmin 只能對「權限為自己子集合」的帳號操作
        if (req.HttpContext.User.FindFirst("is_superadmin")?.Value != "true"
            && !IsOperator(req, user.Id))
            await EnsureTargetIsSubordinateAsync(req.HttpContext.User, user);

        // 必須設定生日才能產生預設密碼
        if (!user.Birthday.HasValue)
            return new BadRequestObjectResult(
                ApiResponse.Fail("此員工尚未設定生日，無法產生預設密碼。請先填寫生日欄位。"));

        // 以生日八碼作為預設密碼（例如：19900101）
        var tempPassword = user.Birthday.Value.ToString("yyyyMMdd");
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
        user.MustChangePassword = true;
        user.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync();

        // 密碼被重設回預設值：舊登入 session 一律作廢
        await RefreshTokenRevoker.RevokeAllAsync(db, user.Id);

        await WriteAuditAsync(req, user.Id, "send_credentials", null);

        // 取得前端登入網址
        var setting = await db.SystemSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync();
        var siteUrl = setting?.SiteUrl?.TrimEnd('/') ?? "https://admin.jabez.com";
        var loginUrl = $"{siteUrl}/auth/login";

        // 寄出通知信：不在信件中明文顯示密碼（Email 為非加密通道、可被轉發、可長期留存於收件匣），
        // 改為告知密碼推導規則（生日 yyyyMMdd 八位數），員工自行對照。
        var subject = "帳號通知 — 請登入並修改密碼";
        var htmlBody = $"""
            <div style="font-family: 'Microsoft JhengHei', sans-serif; max-width: 600px; margin: 0 auto;">
                <h2 style="color: #699F34;">帳號通知</h2>
                <p>{System.Net.WebUtility.HtmlEncode(user.Name)} 您好，</p>
                <p>您的系統帳號已開通，以下為您的登入資訊：</p>
                <table style="border-collapse: collapse; margin: 16px 0;">
                    <tr>
                        <td style="padding: 8px 16px; font-weight: bold; background: #F5F2ED;">Email</td>
                        <td style="padding: 8px 16px; background: #FDFAF5;">{System.Net.WebUtility.HtmlEncode(user.Email)}</td>
                    </tr>
                    <tr>
                        <td style="padding: 8px 16px; font-weight: bold; background: #F5F2ED;">預設密碼</td>
                        <td style="padding: 8px 16px; background: #FDFAF5;">您的<strong>生日八碼數字</strong>（格式：yyyyMMdd，例如 1990 年 5 月 10 日為 <code>19900510</code>）</td>
                    </tr>
                </table>
                <div style="text-align: center; margin: 24px 0;">
                    <a href="{System.Net.WebUtility.HtmlEncode(loginUrl)}" style="display: inline-block; padding: 12px 32px; background-color: #699F34; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 16px;">前往登入系統</a>
                </div>
                <p style="color: #A04040; font-weight: bold;">⚠ 基於安全性考量，請於首次登入後立即修改密碼。</p>
                <p style="color: #6E6F73; font-size: 13px;">若您忘記生日資料或登入有問題，請洽公司 HR 或系統管理員協助。</p>
                <hr style="border: none; border-top: 1px solid #DDD6C8; margin: 24px 0;">
                <p style="color: #A39685; font-size: 12px;">此信件由系統自動寄發，請勿直接回覆。</p>
            </div>
            """;

        await emailService.SendAsync(user.Email, subject, htmlBody);

        return new OkObjectResult(ApiResponse.Ok<object?>(null, "通知信已寄出。"));
    }
}
