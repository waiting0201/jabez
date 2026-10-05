using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace Jabez.Api.Handlers;

public sealed class AuthHandler(
    AppDbContext db,
    IJwtService  jwt,
    IConfiguration config,
    IEmployeeWorkdaysFactory workdaysFactory)
{
    private readonly int _refreshExpiryDays =
        int.TryParse(config["Jwt:RefreshExpiryDays"], out var d) ? d : 7;

    /// <summary>
    /// 帳號不存在時用來「假裝驗證」的 BCrypt 雜湊（工作因子與 HashPassword 預設一致），
    /// 使「帳號不存在」與「密碼錯誤」的回應時間相同。進程內只算一次。
    /// </summary>
    private static readonly string DummyPasswordHash =
        BCrypt.Net.BCrypt.HashPassword("jabez-timing-equalizer-" + Guid.NewGuid());

    public async Task<IActionResult> LoginAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<LoginRequest>();
        if (body is null || string.IsNullOrWhiteSpace(body.Email))
            return new BadRequestObjectResult(ApiResponse.Fail("Email and password are required."));

        var email = LoginAttemptTracker.NormalizeEmail(body.Email);

        // 鎖定檢查放在查使用者之前：鎖定以 Email（含不存在的）為鍵，鎖定中連密碼都不驗，
        // 帳號存在與否在回應上無從區分
        var lockedUntil = await LoginAttemptTracker.GetLockedUntilAsync(db, email);
        if (lockedUntil is not null)
        {
            LoginAttemptTracker.Record(db, req, email, null, false, LoginFailureReasons.Locked);
            await db.SaveChangesAsync();
            var minutes = Math.Max(1, (int)Math.Ceiling((lockedUntil.Value - Clock.Now).TotalMinutes));
            throw new AppException($"登入失敗次數過多，帳號已暫時鎖定，請於 {minutes} 分鐘後再試。", 429);
        }

        // 查詢用戶（含 Roles、Permissions、Department、JobTitle）
        var user = await db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Include(u => u.Department)
            .Include(u => u.JobTitle)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        // 帳號不存在 / 密碼空白也要跑一次 BCrypt（對假雜湊），讓回應時間與「密碼錯誤」一致，
        // 避免以回應時間列舉帳號
        if (user is null || string.IsNullOrWhiteSpace(body.Password))
        {
            BCrypt.Net.BCrypt.Verify(body.Password ?? string.Empty, DummyPasswordHash);
            LoginAttemptTracker.Record(db, req, email, user?.Id, false,
                user is null ? LoginFailureReasons.UnknownEmail : LoginFailureReasons.BadPassword);
            await db.SaveChangesAsync();
            throw AppException.Unauthorized("Invalid email or password.");
        }

        // BCrypt 密碼驗證
        if (!BCrypt.Net.BCrypt.Verify(body.Password, user.PasswordHash))
        {
            LoginAttemptTracker.Record(db, req, email, user.Id, false, LoginFailureReasons.BadPassword);
            await db.SaveChangesAsync();
            throw AppException.Unauthorized("Invalid email or password.");
        }

        if (user.Status == "inactive")
        {
            LoginAttemptTracker.Record(db, req, email, user.Id, false, LoginFailureReasons.Inactive);
            await db.SaveChangesAsync();
            throw AppException.Forbidden("Account is inactive.");
        }

        // 成功登入：紀錄與 Refresh Token 同一次 SaveChanges（見下）。成功列即「失敗計數歸零」的界線。
        LoginAttemptTracker.Record(db, req, email, user.Id, true, null);

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

        var accessToken  = jwt.GenerateAccessToken(user.Id, user.Name, user.Email, roleIds, permissions, user.IsSuperAdmin, user.Department?.Name, user.JobTitle?.Name, user.Department?.Code, user.DepartmentId, user.JobTitle?.Level, user.Avatar, user.AvatarPositionX, user.AvatarPositionY, user.AvatarScale, user.MustChangePassword);
        var refreshToken = jwt.GenerateRefreshToken();

        // 儲存 Refresh Token
        var sessionStartedAt = Clock.Now;
        db.RefreshTokens.Add(new RefreshToken
        {
            Token            = refreshToken,
            UserId           = user.Id,
            ExpiresAt        = DateTime.UtcNow.AddDays(_refreshExpiryDays),
            CreatedAt        = sessionStartedAt,
            SessionStartedAt = sessionStartedAt,
        });

        // 先結束登入主流程的交易：Refresh Token 一定要寫進去，登入才算成功
        await db.SaveChangesAsync();

        // ── 自動補卡（登入是唯一能收斂漏打的時機） ──────────────────
        // 補卡是登入的**副作用**，故獨立一次 SaveChanges：
        // 同帳號併發登入撞 IX_AttendanceRecords_UserId_RecordDate 時，
        // 若與上面的 RefreshToken 共用交易會讓整個登入回 500，使用者直接登不進來。
        var autoClock = AutoClockResult.Empty;
        try
        {
            autoClock = await AttendanceAutoClockService.ApplyAsync(
                db, await workdaysFactory.ForAsync(user.Id), user, permissions.Contains(PermissionCodes.AttendancesWrite));
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // 另一個 session 已補完 → 忽略。補卡邏輯只填空欄，冪等，下次登入自然收斂。
            autoClock = AutoClockResult.Empty;
        }

        return new OkObjectResult(ApiResponse.Ok(new
        {
            access_token  = accessToken,
            refresh_token = refreshToken,
            token_type    = "Bearer",
            must_change_password = user.MustChangePassword,
            auto_clock_in     = autoClock.ClockIn,
            auto_clock_out    = autoClock.ClockOut,
            auto_overtime_end = autoClock.OvertimeEnd,
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

        if (stored is null)
            throw AppException.Unauthorized("Invalid refresh token.");

        if (stored.IsRevoked)
        {
            // 已撤銷的 token 又出現：可能是多分頁同時 refresh 的競態（寬限秒數內只回 401），
            // 也可能是 token 被盜用後與本人各持一份 → 超過寬限一律視為被盜用，連坐撤銷該使用者全部 token
            var withinGrace = stored.RevokedAt is { } revokedAt
                && (Clock.Now - revokedAt).TotalSeconds <= AuthPolicy.RefreshReuseGraceSeconds;
            if (!withinGrace)
                await RefreshTokenRevoker.RevokeAllAsync(db, stored.UserId);
            throw AppException.Unauthorized("Invalid refresh token.");
        }

        if (stored.ExpiresAt < DateTime.UtcNow)
        {
            stored.IsRevoked = true;
            stored.RevokedAt = Clock.Now;
            await db.SaveChangesAsync();
            throw AppException.Unauthorized("Refresh token expired.");
        }

        // 絕對期限：輪替鏈自原始登入起算，超過就必須重新輸入密碼（輪替不延長）
        if (stored.SessionStartedAt.AddDays(AuthPolicy.RefreshAbsoluteDays) < Clock.Now)
        {
            stored.IsRevoked = true;
            stored.RevokedAt = Clock.Now;
            await db.SaveChangesAsync();
            throw AppException.Unauthorized("Session expired. Please sign in again.");
        }

        var user = stored.User;
        if (user.Status == "inactive")
        {
            await RefreshTokenRevoker.RevokeAllAsync(db, user.Id);
            throw AppException.Forbidden("Account is inactive.");
        }

        // 撤銷舊 token，發行新 token
        stored.IsRevoked = true;
        stored.RevokedAt = Clock.Now;

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

        var newAccess  = jwt.GenerateAccessToken(user.Id, user.Name, user.Email, roleIds, permissions, user.IsSuperAdmin, user.Department?.Name, user.JobTitle?.Name, user.Department?.Code, user.DepartmentId, user.JobTitle?.Level, user.Avatar, user.AvatarPositionX, user.AvatarPositionY, user.AvatarScale, user.MustChangePassword);
        var newRefresh = jwt.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Token            = newRefresh,
            UserId           = user.Id,
            ExpiresAt        = DateTime.UtcNow.AddDays(_refreshExpiryDays),
            SessionStartedAt = stored.SessionStartedAt,   // 原樣帶下去，絕對期限不因輪替延長
        });
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok(new
        {
            access_token  = newAccess,
            refresh_token = newRefresh,
            token_type    = "Bearer",
        }, "Token refreshed."));
    }

    /// <summary>
    /// 登出：撤銷傳入的 Refresh Token（公開路由 —— 登出時 Access Token 可能已過期，由 Refresh Token 本身證明身分）。
    /// 一律回 200，不洩漏 token 是否存在 / 已撤銷。
    /// </summary>
    public async Task<IActionResult> LogoutAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<RefreshRequest>();
        if (body is not null && !string.IsNullOrWhiteSpace(body.RefreshToken))
        {
            var stored = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == body.RefreshToken);
            if (stored is { IsRevoked: false })
            {
                stored.IsRevoked = true;
                stored.RevokedAt = Clock.Now;
                await db.SaveChangesAsync();
            }
        }
        return new OkObjectResult(ApiResponse.Ok<object?>(null, "Logged out."));
    }

    /// <summary>
    /// 修改密碼（需登入，驗證舊密碼後更新）。
    /// 成功後撤銷該使用者**全部** Refresh Token（含目前這個登入）—— 前端改密碼後本就會登出重登。
    /// </summary>
    public async Task<IActionResult> ChangePasswordAsync(HttpRequest req)
    {
        var principal = await jwt.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw AppException.Unauthorized("Invalid token claims.");

        var body = await req.ReadFromJsonAsync<ChangePasswordRequest>();
        if (body is null || string.IsNullOrWhiteSpace(body.CurrentPassword) || string.IsNullOrWhiteSpace(body.NewPassword))
            return new BadRequestObjectResult(ApiResponse.Fail("舊密碼與新密碼為必填。"));

        if (body.NewPassword.Length < AuthPolicy.PasswordMinLength)
            return new BadRequestObjectResult(ApiResponse.Fail($"新密碼長度至少 {AuthPolicy.PasswordMinLength} 碼。"));

        var user = await db.Users.FindAsync(userId)
            ?? throw AppException.NotFound("使用者不存在。");

        if (!BCrypt.Net.BCrypt.Verify(body.CurrentPassword, user.PasswordHash))
            return new BadRequestObjectResult(ApiResponse.Fail("舊密碼不正確。"));

        // 不可與舊密碼相同（舊密碼剛驗證過，直接比對明文即可）
        if (body.NewPassword == body.CurrentPassword)
            return new BadRequestObjectResult(ApiResponse.Fail("新密碼不可與舊密碼相同。"));

        // 不可等於預設密碼（生日 yyyyMMdd）：預設密碼是公開推導規則，改成它等於沒改
        if (user.Birthday.HasValue && body.NewPassword == user.Birthday.Value.ToString("yyyyMMdd"))
            return new BadRequestObjectResult(ApiResponse.Fail("新密碼不可使用預設密碼（生日八碼）。"));

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await RefreshTokenRevoker.RevokeAllAsync(db, userId);

        return new OkObjectResult(ApiResponse.Ok<object?>(null, "密碼修改成功。"));
    }
}
