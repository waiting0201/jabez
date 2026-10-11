using Jabez.Api.Common;
using Jabez.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 撤銷 Refresh Token 的共用入口（static，比照 LeaveRevocationService 風格）。
/// 一次 UPDATE 完成、立即生效，**不依賴也不呼叫 SaveChanges**。
/// <para>
/// 呼叫時機（使用者帳號安全事件）：改密碼 / 管理員重設或設定他人密碼 / 寄帳號通知信 /
/// 帳號停用 / 角色變更 / 偵測到已撤銷 token 被重用。
/// </para>
/// 同時換新 <c>Users.SecurityStamp</c>，AppRouter 比對 token 內 sstamp claim，舊 Access Token 立即失效（回 401）。
/// 只想讓舊 Access Token 失效、但不想登出對方（改部門 / 職稱：refresh 可無縫換到新 claims）時，用 <see cref="BumpSecurityStampAsync"/>。
/// </summary>
public static class RefreshTokenRevoker
{
    /// <summary>換新安全戳記（一次 UPDATE，不呼叫 SaveChanges）：使該員所有已簽發的 Access Token 立即失效。</summary>
    public static Task<int> BumpSecurityStampAsync(AppDbContext db, Guid userId)
    {
        var stamp = Guid.NewGuid();
        return db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.SecurityStamp, stamp));
    }

    public static async Task<int> RevokeAllAsync(AppDbContext db, Guid userId)
    {
        await BumpSecurityStampAsync(db, userId);
        var now = Clock.Now;
        return await db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(rt => rt.IsRevoked, true)
                .SetProperty(rt => rt.RevokedAt, (DateTime?)now));
    }
}
