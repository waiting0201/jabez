using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Handlers;

public sealed class UserHandler(AppDbContext db, IUserReadService reader, IEmailService emailService, IBlobStorageService blob)
{
    // GET /api/users/lookup — 輕量級使用者清單（供指定審核者下拉選單，不需 users:read 權限）
    public async Task<IActionResult> GetLookupAsync()
    {
        var list = await reader.GetLookupAsync();
        return new OkObjectResult(ApiResponse.Ok(list));
    }

    // GET /api/users — Dapper 讀取（含 JOIN）
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        // 有分頁參數 → 回傳 PagedResult；無分頁參數 → 回傳平面陣列（供下拉選單用）
        if (req.Query.ContainsKey("page") || req.Query.ContainsKey("pageSize"))
        {
            int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
            int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
            var result = await reader.GetPagedAsync(page, pageSize);
            return new OkObjectResult(ApiResponse.Ok(result));
        }

        var all = await reader.GetAllAsync();
        return new OkObjectResult(ApiResponse.Ok(all));
    }

    // GET /api/users/{id}
    public async Task<IActionResult> GetByIdAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid user ID format."));

        // 超管帳號不對外暴露
        if (await db.Users.AnyAsync(u => u.Id == guid && u.IsSuperAdmin))
            return new NotFoundObjectResult(ApiResponse.Fail("User not found.", $"No user with id '{id}'."));

        var user = await reader.GetByIdAsync(guid);
        return user is null
            ? new NotFoundObjectResult(ApiResponse.Fail("User not found.", $"No user with id '{id}'."))
            : new OkObjectResult(ApiResponse.Ok(user));
    }

    private const string SignatureContainer = "signatures";
    private static readonly string[] AllowedSignatureTypes = ["image/png", "image/jpeg", "image/gif", "image/webp"];

    /// <summary>
    /// 處理簽名檔上傳（新增/更新共用）。
    /// 上傳成功後回傳 API 代理路徑（/files/signatures/{blobName}），
    /// 而非直接回傳 Blob URL，避免私有容器 403 及 CORS 問題。
    /// </summary>
    private async Task<string?> HandleSignatureUploadAsync(IFormFileCollection files, Guid userId, string? existingUrl)
    {
        var file = files.GetFile("signature");
        if (file is null || file.Length == 0) return existingUrl;

        if (!AllowedSignatureTypes.Contains(file.ContentType.ToLower()))
            throw AppException.BadRequest("僅支援 PNG、JPEG、GIF、WebP 圖片格式。");

        // 刪除舊簽名檔：同時支援舊 Blob URL 格式與新 API 代理路徑格式
        var oldBlobName = existingUrl is not null && existingUrl.StartsWith("files/signatures/", StringComparison.OrdinalIgnoreCase)
            ? existingUrl["files/signatures/".Length..]
            : blob.ExtractBlobName(existingUrl, SignatureContainer);
        if (oldBlobName is not null)
            await blob.DeleteAsync(SignatureContainer, oldBlobName);

        var ext = Path.GetExtension(file.FileName);
        var blobName = $"{userId}{ext}";
        using var stream = file.OpenReadStream();

        // 上傳至 Blob Storage，回傳 API 代理路徑供前端使用
        await blob.UploadAsync(SignatureContainer, blobName, stream, file.ContentType);
        return $"files/signatures/{blobName}";
    }

    // POST /api/users
    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var form = await req.ReadFormAsync();

        var name     = form["name"].ToString();
        var email    = form["email"].ToString();
        var password = form["password"].ToString();
        var avatar   = form["avatar"].ToString();
        var status   = form["status"].ToString();
        var roleIdsRaw = form["roleIds"].ToArray();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            return new BadRequestObjectResult(ApiResponse.Fail("Name and Email are required."));

        if (!string.IsNullOrWhiteSpace(password) && password.Length < 6)
            return new BadRequestObjectResult(ApiResponse.Fail("Password must be at least 6 characters."));

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

        var userId = Guid.NewGuid();

        var user = new User
        {
            Id           = userId,
            Name         = name,
            Email        = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(effectivePassword),
            Avatar       = string.IsNullOrEmpty(avatar) ? null : avatar,
            Status       = string.IsNullOrEmpty(status) ? "active" : status,
            DepartmentId = int.TryParse(form["departmentId"], out var did) && did > 0 ? did : null,
            JobTitleId   = int.TryParse(form["jobTitleId"], out var jtid) && jtid > 0 ? jtid : null,
            HireDate     = DateTime.TryParse(form["hireDate"], out var hd) ? hd : null,
            ResignDate   = DateTime.TryParse(form["resignDate"], out var rd) ? rd : null,
            BaseSalary    = decimal.TryParse(form["baseSalary"], out var bs) ? bs : null,
            MealAllowance = decimal.TryParse(form["mealAllowance"], out var ma) ? ma : null,
            OvertimePay   = decimal.TryParse(form["overtimePay"], out var op) ? op : null,
            SendPaySlip   = form["sendPaySlip"] == "true",
            AgentUserId   = Guid.TryParse(form["agentUserId"], out var aid) && aid != Guid.Empty ? aid : null,
            Birthday     = birthday,
            CreatedAt    = Clock.Now,
            UpdatedAt    = Clock.Now,
        };

        // 處理簽名檔
        user.SignatureUrl = await HandleSignatureUploadAsync(form.Files, userId, null);

        db.Users.Add(user);

        // 處理 Roles
        foreach (var roleId in roleIdsRaw)
            if (!string.IsNullOrEmpty(roleId))
                db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(user.Id);
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

        var nameVal     = form["name"].ToString();
        var emailVal    = form["email"].ToString();
        var avatarVal   = form["avatar"].ToString();
        var statusVal   = form["status"].ToString();
        var passwordVal = form["password"].ToString();

        if (!string.IsNullOrEmpty(nameVal))     user.Name   = nameVal;
        if (!string.IsNullOrEmpty(emailVal))    user.Email  = emailVal;
        if (!string.IsNullOrEmpty(avatarVal))   user.Avatar = avatarVal;
        if (!string.IsNullOrEmpty(statusVal))   user.Status = statusVal;
        if (!string.IsNullOrEmpty(passwordVal)) user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordVal);

        if (form.ContainsKey("departmentId"))
            user.DepartmentId = int.TryParse(form["departmentId"], out var did) && did > 0 ? did : null;
        if (form.ContainsKey("jobTitleId"))
            user.JobTitleId = int.TryParse(form["jobTitleId"], out var jtid) && jtid > 0 ? jtid : null;
        if (form.ContainsKey("hireDate"))
            user.HireDate = DateTime.TryParse(form["hireDate"], out var hd) ? hd : null;
        if (form.ContainsKey("resignDate"))
            user.ResignDate = DateTime.TryParse(form["resignDate"], out var rd) ? rd : null;
        if (form.ContainsKey("baseSalary"))
            user.BaseSalary = decimal.TryParse(form["baseSalary"], out var bs) ? bs : null;
        if (form.ContainsKey("mealAllowance"))
            user.MealAllowance = decimal.TryParse(form["mealAllowance"], out var ma) ? ma : null;
        if (form.ContainsKey("overtimePay"))
            user.OvertimePay = decimal.TryParse(form["overtimePay"], out var op) ? op : null;
        if (form.ContainsKey("sendPaySlip"))
            user.SendPaySlip = form["sendPaySlip"] == "true";
        if (form.ContainsKey("agentUserId"))
            user.AgentUserId = Guid.TryParse(form["agentUserId"], out var aid) && aid != Guid.Empty ? aid : null;
        if (form.ContainsKey("birthday"))
            user.Birthday = DateTime.TryParse(form["birthday"], out var bd) ? bd : null;

        // 處理簽名檔：removeSignature=true 表示刪除
        if (form["removeSignature"] == "true")
        {
            var oldBlobName = blob.ExtractBlobName(user.SignatureUrl, SignatureContainer);
            if (oldBlobName is not null)
                await blob.DeleteAsync(SignatureContainer, oldBlobName);
            user.SignatureUrl = null;
        }
        else
        {
            user.SignatureUrl = await HandleSignatureUploadAsync(form.Files, guid, user.SignatureUrl);
        }

        user.UpdatedAt = Clock.Now;

        // 更新 Roles
        if (form.ContainsKey("roleIds"))
        {
            var roleIds = form["roleIds"].ToArray();
            db.UserRoles.RemoveRange(user.UserRoles);
            foreach (var roleId in roleIds)
                if (!string.IsNullOrEmpty(roleId))
                    db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(user.Id);
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

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"User '{id}' deleted."));
    }

    /// <summary>
    /// 寄出員工帳號通知信。
    /// 預設密碼為員工生日八碼（yyyyMMdd），員工首次登入後須立即修改密碼。
    /// 若員工尚未設定生日，則無法寄出通知信。
    /// </summary>
    public async Task<IActionResult> SendCredentialsAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid user ID format."));

        var user = await db.Users.FindAsync(guid)
            ?? throw AppException.NotFound("User");

        if (user.IsSuperAdmin)
            throw AppException.Forbidden("Cannot send credentials for the system super admin account.");

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

        // 取得前端登入網址
        var setting = await db.SystemSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync();
        var siteUrl = setting?.SiteUrl?.TrimEnd('/') ?? "https://admin.jabez.com";
        var loginUrl = $"{siteUrl}/auth/login";

        // 寄出通知信（不在信件中明文顯示密碼，僅提示格式）
        var subject = "帳號通知 — 請登入並修改密碼";
        var htmlBody = $"""
            <div style="font-family: 'Microsoft JhengHei', sans-serif; max-width: 600px; margin: 0 auto;">
                <h2 style="color: #699F34;">帳號通知</h2>
                <p>{user.Name} 您好，</p>
                <p>您的系統帳號已開通，以下為您的登入資訊：</p>
                <table style="border-collapse: collapse; margin: 16px 0;">
                    <tr>
                        <td style="padding: 8px 16px; font-weight: bold; background: #F5F2ED;">Email</td>
                        <td style="padding: 8px 16px; background: #FDFAF5;">{user.Email}</td>
                    </tr>
                    <tr>
                        <td style="padding: 8px 16px; font-weight: bold; background: #F5F2ED;">預設密碼</td>
                        <td style="padding: 8px 16px; background: #FDFAF5; font-family: monospace; font-size: 16px;">{tempPassword}</td>
                    </tr>
                </table>
                <div style="text-align: center; margin: 24px 0;">
                    <a href="{loginUrl}" style="display: inline-block; padding: 12px 32px; background-color: #699F34; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 16px;">前往登入系統</a>
                </div>
                <p style="color: #A04040; font-weight: bold;">⚠ 基於安全性考量，請於首次登入後立即修改密碼。</p>
                <hr style="border: none; border-top: 1px solid #DDD6C8; margin: 24px 0;">
                <p style="color: #A39685; font-size: 12px;">此信件由系統自動寄發，請勿直接回覆。</p>
            </div>
            """;

        await emailService.SendAsync(user.Email, subject, htmlBody);

        return new OkObjectResult(ApiResponse.Ok<object?>(null, "通知信已寄出。"));
    }
}
