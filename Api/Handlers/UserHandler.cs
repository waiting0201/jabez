using System.Security.Cryptography;
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

public sealed class UserHandler(AppDbContext db, IUserReadService reader, IEmailService emailService)
{
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

    // POST /api/users
    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<CreateUserRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.Email))
            return new BadRequestObjectResult(ApiResponse.Fail("Name and Email are required."));

        if (string.IsNullOrWhiteSpace(body.Password) || body.Password.Length < 6)
            return new BadRequestObjectResult(ApiResponse.Fail("Password is required and must be at least 6 characters."));

        // 檢查 Email 唯一
        if (await db.Users.AnyAsync(u => u.Email.ToLower() == body.Email.ToLower()))
            throw AppException.Conflict($"Email '{body.Email}' is already in use.");

        var user = new User
        {
            Id           = Guid.NewGuid(),
            Name         = body.Name,
            Email        = body.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.Password),
            Avatar       = body.Avatar,
            Status       = body.Status,
            DepartmentId = body.DepartmentId,
            JobTitleId   = body.JobTitleId,
            HireDate     = body.HireDate,
            ResignDate   = body.ResignDate,
            BaseSalary   = body.BaseSalary,
            AgentUserId  = body.AgentUserId,
            CreatedAt    = Clock.Now,
            UpdatedAt    = Clock.Now,
        };
        db.Users.Add(user);

        // 處理 Roles
        foreach (var roleId in body.RoleIds)
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

        var body = await req.ReadFromJsonAsync<UpdateUserRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == guid)
            ?? throw AppException.NotFound("User");

        if (user.IsSuperAdmin)
            throw AppException.Forbidden("Cannot modify the system super admin account.");

        if (body.Name         is not null) user.Name         = body.Name;
        if (body.Email        is not null) user.Email        = body.Email;
        if (body.Avatar       is not null) user.Avatar       = body.Avatar;
        if (body.Status       is not null) user.Status       = body.Status;
        if (body.Password     is not null) user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.Password);
        if (body.DepartmentId.HasValue)    user.DepartmentId = body.DepartmentId == 0 ? null : body.DepartmentId;
        if (body.JobTitleId.HasValue)      user.JobTitleId   = body.JobTitleId   == 0 ? null : body.JobTitleId;
        if (body.HireDate.HasValue)        user.HireDate     = body.HireDate;
        if (body.ResignDate.HasValue)      user.ResignDate   = body.ResignDate;
        if (body.BaseSalary.HasValue)      user.BaseSalary   = body.BaseSalary;
        if (body.AgentUserId.HasValue)     user.AgentUserId  = body.AgentUserId == Guid.Empty ? null : body.AgentUserId;
        user.UpdatedAt = Clock.Now;

        // 更新 Roles
        if (body.RoleIds is not null)
        {
            db.UserRoles.RemoveRange(user.UserRoles);
            foreach (var roleId in body.RoleIds)
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

    /// <summary>寄出員工帳號通知信（產生臨時密碼、設定首次登入須改密碼）</summary>
    public async Task<IActionResult> SendCredentialsAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid user ID format."));

        var user = await db.Users.FindAsync(guid)
            ?? throw AppException.NotFound("User");

        if (user.IsSuperAdmin)
            throw AppException.Forbidden("Cannot send credentials for the system super admin account.");

        // 產生 12 碼隨機密碼
        var tempPassword = GenerateRandomPassword(12);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
        user.MustChangePassword = true;
        user.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync();

        // 寄出通知信
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
                        <td style="padding: 8px 16px; font-weight: bold; background: #F5F2ED;">臨時密碼</td>
                        <td style="padding: 8px 16px; background: #FDFAF5; font-family: monospace; font-size: 16px;">{tempPassword}</td>
                    </tr>
                </table>
                <p style="color: #A04040; font-weight: bold;">⚠ 基於安全性考量，請於首次登入後立即修改密碼。</p>
                <hr style="border: none; border-top: 1px solid #DDD6C8; margin: 24px 0;">
                <p style="color: #A39685; font-size: 12px;">此信件由系統自動寄發，請勿直接回覆。</p>
            </div>
            """;

        await emailService.SendAsync(user.Email, subject, htmlBody);

        return new OkObjectResult(ApiResponse.Ok<object?>(null, "通知信已寄出。"));
    }

    private static string GenerateRandomPassword(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        return string.Create(length, chars, (span, c) =>
        {
            var bytes = RandomNumberGenerator.GetBytes(length);
            for (int i = 0; i < span.Length; i++)
                span[i] = c[bytes[i] % c.Length];
        });
    }
}
