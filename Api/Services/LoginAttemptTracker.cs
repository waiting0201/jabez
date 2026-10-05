using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 登入嘗試紀錄 + 登入失敗鎖定判定（static，不依賴 DI 狀態）。
/// <para>
/// **鎖定規則**：同一個 Email 在最近 <see cref="AuthPolicy.LockoutMinutes"/> 分鐘內、
/// 自最近一次成功登入之後，累積 <see cref="AuthPolicy.MaxFailedLogins"/> 次「計入鎖定」的失敗
/// （密碼錯誤 / 帳號不存在）→ 鎖到「最後一次失敗 + LockoutMinutes」。成功登入即歸零。
/// </para>
/// <para>
/// 鎖定以 **Email（含不存在的）** 為鍵，而不是 User 上的計數欄：
/// 若只鎖真實帳號，攻擊者可由「第 5 次起回鎖定訊息」判斷某 Email 存在；以 Email 為鍵則兩者行為一致。
/// 代價是任何人都能對某 Email 連錯 5 次把該員鎖 15 分鐘（DoS），為鎖定機制的固有取捨。
/// </para>
/// 不使用 IP 當鎖定鍵：X-Forwarded-For 可偽造，且辦公室同出口 IP 會連坐。IP 只記錄供稽核。
/// </summary>
public static class LoginAttemptTracker
{
    private const int MaxEmailLength = 256;

    /// <summary>Email 正規化（trim + 小寫 + 截斷），與寫入紀錄、查鎖定共用同一份</summary>
    public static string NormalizeEmail(string? email)
    {
        var e = (email ?? string.Empty).Trim().ToLowerInvariant();
        return e.Length <= MaxEmailLength ? e : e[..MaxEmailLength];
    }

    /// <summary>目前是否鎖定中；是則回傳解鎖時間（Clock.Now 基準），否則 null</summary>
    public static async Task<DateTime?> GetLockedUntilAsync(AppDbContext db, string normalizedEmail)
    {
        var now   = Clock.Now;
        var since = now.AddMinutes(-AuthPolicy.LockoutMinutes);

        // 視窗內由新到舊；遇到成功登入就停止計數（成功歸零）。視窗至多 15 分鐘，資料量很小。
        var recent = await db.LoginAttempts
            .AsNoTracking()
            .Where(a => a.Email == normalizedEmail && a.AttemptedAt > since)
            .OrderByDescending(a => a.AttemptedAt)
            .Select(a => new { a.Succeeded, a.FailureReason, a.AttemptedAt })
            .ToListAsync();

        var counted = new List<DateTime>();
        foreach (var a in recent)
        {
            if (a.Succeeded) break;
            if (a.FailureReason is LoginFailureReasons.BadPassword or LoginFailureReasons.UnknownEmail)
                counted.Add(a.AttemptedAt);
        }

        if (counted.Count < AuthPolicy.MaxFailedLogins) return null;

        var until = counted[0].AddMinutes(AuthPolicy.LockoutMinutes);
        return until > now ? until : null;
    }

    /// <summary>登記一次嘗試（只 Add，不 SaveChanges；呼叫端在丟例外前須自行 SaveChanges）</summary>
    public static void Record(
        AppDbContext db, HttpRequest req, string normalizedEmail, Guid? userId, bool succeeded, string? failureReason)
    {
        db.LoginAttempts.Add(new LoginAttempt
        {
            Email         = normalizedEmail,
            UserId        = userId,
            Succeeded     = succeeded,
            FailureReason = failureReason,
            IpAddress     = ClientInfo.ResolveIp(req),
            UserAgent     = ClientInfo.ResolveUserAgent(req),
            AttemptedAt   = Clock.Now,
        });
    }
}
