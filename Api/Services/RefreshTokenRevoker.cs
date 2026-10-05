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
/// ⚠ 只能讓 Refresh Token 立即失效；已簽發的 Access Token（60 分鐘）無法收回，
/// 因為 AppRouter 驗 JWT 是無狀態的（不查 DB）。
/// </summary>
public static class RefreshTokenRevoker
{
    public static Task<int> RevokeAllAsync(AppDbContext db, Guid userId)
    {
        var now = Clock.Now;
        return db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(rt => rt.IsRevoked, true)
                .SetProperty(rt => rt.RevokedAt, (DateTime?)now));
    }
}
