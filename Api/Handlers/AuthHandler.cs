using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace Jabez.Api.Handlers;

public sealed class AuthHandler(
    AppDbContext db,
    IJwtService  jwt,
    IConfiguration config)
{
    private readonly int _refreshExpiryDays =
        int.TryParse(config["Jwt:RefreshExpiryDays"], out var d) ? d : 7;

    public async Task<IActionResult> LoginAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<LoginRequest>();
        if (body is null || string.IsNullOrWhiteSpace(body.Email))
            return new BadRequestObjectResult(ApiResponse.Fail("Email and password are required."));

        // 查詢用戶（含 Roles、Permissions、Department、JobTitle）
        var user = await db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Include(u => u.Department)
            .Include(u => u.JobTitle)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == body.Email.ToLower());

        if (user is null || string.IsNullOrWhiteSpace(body.Password))
            throw AppException.Unauthorized("Invalid email or password.");

        // BCrypt 密碼驗證
        if (!BCrypt.Net.BCrypt.Verify(body.Password, user.PasswordHash))
            throw AppException.Unauthorized("Invalid email or password.");

        if (user.Status == "inactive")
            throw AppException.Forbidden("Account is inactive.");

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToArray();
        string[] permissions;
        if (user.IsSuperAdmin)
        {
            // 超管帳號：直接從 DB 取得所有 Permission code，不受角色異動影響
            permissions = await db.Permissions.Select(p => p.Code).ToArrayAsync();
        }
        else
        {
            permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Code)
                .Distinct()
                .ToArray();
        }

        var accessToken  = jwt.GenerateAccessToken(user.Id, user.Name, user.Email, roleIds, permissions, user.IsSuperAdmin, user.Department?.Name, user.JobTitle?.Name, user.Department?.Code, user.DepartmentId, user.JobTitle?.Level, user.Avatar, user.AvatarPositionX, user.AvatarPositionY, user.AvatarScale);
        var refreshToken = jwt.GenerateRefreshToken();

        // 儲存 Refresh Token
        db.RefreshTokens.Add(new RefreshToken
        {
            Token     = refreshToken,
            UserId    = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshExpiryDays),
        });

        // ── 自動補打下班卡（歷史漏打紀錄） ──────────────────────────
        var today = Clock.Now.Date;
        var pendingRecords = await db.AttendanceRecords
            .Where(a => a.UserId == user.Id
                && a.ClockInTime != null
                && a.ClockOutTime == null
                && a.RecordDate < today)
            .ToListAsync();

        AutoClockOutInfo? autoClockOutInfo = null;
        if (pendingRecords.Count > 0)
        {
            var setting = await db.SystemSettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            var workEndTime = TimeSpan.TryParse(setting?.WorkEndTime, out var t) ? t : new TimeSpan(18, 0, 0);

            foreach (var record in pendingRecords)
            {
                record.ClockOutTime = record.RecordDate.Date + workEndTime;
            }

            autoClockOutInfo = new AutoClockOutInfo(
                pendingRecords.Count,
                pendingRecords.Select(r => r.RecordDate.ToString("yyyy-MM-dd")).OrderBy(d => d).ToArray());
        }

        // ── 自動補打加班結束卡（已開始加班但未打結束卡） ──────────────
        var pendingOvertimeRecords = await db.AttendanceRecords
            .Include(a => a.OvertimeRequest)
            .Where(a => a.UserId == user.Id
                && a.OvertimeStartTime != null
                && a.OvertimeEndTime == null
                && a.RecordDate < today)
            .ToListAsync();

        AutoOvertimeEndInfo? autoOvertimeEndInfo = null;
        if (pendingOvertimeRecords.Count > 0)
        {
            foreach (var record in pendingOvertimeRecords)
            {
                var hours = (double)(record.OvertimeRequest?.EstimatedHours ?? 0);
                record.OvertimeEndTime = record.OvertimeStartTime!.Value.AddHours(hours);
            }

            autoOvertimeEndInfo = new AutoOvertimeEndInfo(
                pendingOvertimeRecords.Count,
                pendingOvertimeRecords.Select(r => r.RecordDate.ToString("yyyy-MM-dd")).OrderBy(d => d).ToArray());
        }

        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok(new
        {
            access_token  = accessToken,
            refresh_token = refreshToken,
            token_type    = "Bearer",
            must_change_password = user.MustChangePassword,
            auto_clock_out = autoClockOutInfo,
            auto_overtime_end = autoOvertimeEndInfo,
        }, "Login successful."));
    }

    public async Task<IActionResult> RefreshAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<RefreshRequest>();
        if (body is null || string.IsNullOrWhiteSpace(body.RefreshToken))
            return new BadRequestObjectResult(ApiResponse.Fail("RefreshToken is required."));

        var stored = await db.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
            .Include(rt => rt.User)
                .ThenInclude(u => u.Department)
            .Include(rt => rt.User)
                .ThenInclude(u => u.JobTitle)
            .FirstOrDefaultAsync(rt => rt.Token == body.RefreshToken);

        if (stored is null || stored.IsRevoked)
            throw AppException.Unauthorized("Invalid refresh token.");

        if (stored.ExpiresAt < DateTime.UtcNow)
        {
            stored.IsRevoked = true;
            await db.SaveChangesAsync();
            throw AppException.Unauthorized("Refresh token expired.");
        }

        var user = stored.User;
        if (user.Status == "inactive")
            throw AppException.Forbidden("Account is inactive.");

        // 撤銷舊 token，發行新 token
        stored.IsRevoked = true;

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToArray();
        string[] permissions;
        if (user.IsSuperAdmin)
        {
            permissions = await db.Permissions.Select(p => p.Code).ToArrayAsync();
        }
        else
        {
            permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Code)
                .Distinct()
                .ToArray();
        }

        var newAccess  = jwt.GenerateAccessToken(user.Id, user.Name, user.Email, roleIds, permissions, user.IsSuperAdmin, user.Department?.Name, user.JobTitle?.Name, user.Department?.Code, user.DepartmentId, user.JobTitle?.Level, user.Avatar, user.AvatarPositionX, user.AvatarPositionY, user.AvatarScale);
        var newRefresh = jwt.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Token     = newRefresh,
            UserId    = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshExpiryDays),
        });
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok(new
        {
            access_token  = newAccess,
            refresh_token = newRefresh,
            token_type    = "Bearer",
        }, "Token refreshed."));
    }

    /// <summary>修改密碼（需登入，驗證舊密碼後更新）</summary>
    public async Task<IActionResult> ChangePasswordAsync(HttpRequest req)
    {
        var principal = await jwt.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw AppException.Unauthorized("Invalid token claims.");

        var body = await req.ReadFromJsonAsync<ChangePasswordRequest>();
        if (body is null || string.IsNullOrWhiteSpace(body.CurrentPassword) || string.IsNullOrWhiteSpace(body.NewPassword))
            return new BadRequestObjectResult(ApiResponse.Fail("舊密碼與新密碼為必填。"));

        if (body.NewPassword.Length < 6)
            return new BadRequestObjectResult(ApiResponse.Fail("新密碼長度至少 6 碼。"));

        var user = await db.Users.FindAsync(userId)
            ?? throw AppException.NotFound("使用者不存在。");

        if (!BCrypt.Net.BCrypt.Verify(body.CurrentPassword, user.PasswordHash))
            return new BadRequestObjectResult(ApiResponse.Fail("舊密碼不正確。"));

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok<object?>(null, "密碼修改成功。"));
    }
}
