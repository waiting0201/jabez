using System.Security.Cryptography;
using System.Text;
using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Jabez.Api.Services;

/// <summary>
/// 防機器人打卡（2026-10 hotfix，起因：正式站有人以排程腳本「登入 → 1 秒內打卡」、且不送 GPS）。
///
/// 兩道關卡，四個本人打卡動作（上班 / 下班 / 加班開始 / 加班結束）共用：
///   1. ~~強制 GPS~~：正式站 2026-10-05 起取消（無法取得定位的同仁無從打卡），GPS 改為選填、照常記錄。
///   2. **一次性挑戰碼**：打卡前須先呼叫 POST /attendances/clock-challenge 取碼，
///      碼以 HMAC 簽章綁定「使用者 + 動作 + 簽發時間 + nonce」，
///      **簽發後至少 <see cref="MinAgeMs"/> 毫秒、至多 <see cref="MaxAgeMs"/> 毫秒內**才能使用，且只能成功使用一次。
///      前端在取得 GPS 的同時取碼、不足最短時間時自動補等，正常使用者不會察覺。
///   3. **嘗試紀錄**：成功與被擋下的每一次嘗試都寫入 AttendancePunchLogs（IP / User-Agent / GPS / 停留時間）。
///
/// ⚠ 這是「提高門檻 + 留證據」，不是絕對防護：改寫過的腳本仍可模仿取碼與等待。
///   真正無法以腳本繞過的只有人機驗證或生物辨識（passkey），日後若仍有濫用再評估。
/// 系統自動補卡（AttendanceAutoClockService）與管理者修改（PUT /attendances/{id}）不經此流程。
/// </summary>
public sealed class AttendancePunchGuard(AppDbContext db, IConfiguration config) : IAttendancePunchGuard
{
    /// <summary>挑戰碼最短停留時間：腳本「取碼後立刻打卡」會被擋</summary>
    public const int MinAgeMs = 3000;

    /// <summary>挑戰碼有效時間</summary>
    public const int MaxAgeMs = 5 * 60 * 1000;

    /// <summary>可打卡的四個動作（與路由 /attendances/{action} 同名）</summary>
    public static readonly HashSet<string> Actions = ["clock-in", "clock-out", "overtime-start", "overtime-end"];

    /// <summary>擋下原因代碼（寫入 AttendancePunchLog.BlockReason）</summary>
    public static class BlockReasons
    {
        /// <summary>已停用（GPS 改為選填），保留供判讀既有紀錄</summary>
        public const string NoGps            = "no_gps";
        public const string ChallengeMissing = "challenge_missing";
        public const string ChallengeInvalid = "challenge_invalid";
        public const string TooFast          = "too_fast";
        public const string ChallengeExpired = "challenge_expired";
        public const string ChallengeReused  = "challenge_reused";
    }

    private readonly byte[] _key = DeriveKey(config["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is required."));

    public ClockChallengeDto IssueChallenge(Guid userId, string action)
    {
        if (!Actions.Contains(action))
            throw AppException.BadRequest("不支援的打卡動作。");

        var issuedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nonce    = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var payload  = $"{userId:N}.{action}.{issuedMs}.{nonce}";
        var token    = $"{B64Url(Encoding.UTF8.GetBytes(payload))}.{B64Url(Sign(payload))}";

        return new ClockChallengeDto(token, MinAgeMs, MaxAgeMs);
    }

    public async Task GuardAsync(HttpRequest req, Guid userId, string action, ClockActionRequest body)
    {
        var log = new AttendancePunchLog
        {
            UserId      = userId,
            Action      = action,
            AttemptedAt = Clock.Now,
            Latitude    = body.Latitude,
            Longitude   = body.Longitude,
            Accuracy    = body.Accuracy,
            IpAddress   = Truncate(ResolveClientIp(req), 64),
            UserAgent   = Truncate(req.Headers.UserAgent.ToString(), 512),
        };

        var (reason, message) = await EvaluateAsync(userId, action, body, log);

        log.Succeeded   = reason is null;
        log.BlockReason = reason;
        db.AttendancePunchLogs.Add(log);

        if (reason is null) return;

        await db.SaveChangesAsync();
        throw AppException.BadRequest(message!);
    }

    private async Task<(string? Reason, string? Message)> EvaluateAsync(
        Guid userId, string action, ClockActionRequest body, AttendancePunchLog log)
    {
        // GPS 為選填（正式站 2026-10-05 取消強制），有帶就照常記錄於 log 與打卡紀錄

        // 挑戰碼
        if (string.IsNullOrWhiteSpace(body.ChallengeToken))
            return (BlockReasons.ChallengeMissing, "打卡驗證失敗，請重新整理頁面後再試。");

        if (!TryParse(body.ChallengeToken, out var tUser, out var tAction, out var issuedMs, out var nonce))
            return (BlockReasons.ChallengeInvalid, "打卡驗證失敗，請重新整理頁面後再試。");

        log.ChallengeNonce = nonce;

        if (tUser != userId || tAction != action)
            return (BlockReasons.ChallengeInvalid, "打卡驗證失敗，請重新整理頁面後再試。");

        var ageMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - issuedMs;
        log.ChallengeAgeMs = (int)Math.Clamp(ageMs, int.MinValue, int.MaxValue);

        if (ageMs < MinAgeMs)
            return (BlockReasons.TooFast, "操作過快，請稍候幾秒再打卡。");
        if (ageMs > MaxAgeMs)
            return (BlockReasons.ChallengeExpired, "打卡驗證已逾時，請重新整理頁面後再試。");

        if (await db.AttendancePunchLogs.AnyAsync(l => l.Succeeded && l.ChallengeNonce == nonce))
            return (BlockReasons.ChallengeReused, "打卡驗證已使用過，請重新整理頁面後再試。");

        return (null, null);
    }

    private bool TryParse(string token, out Guid userId, out string action, out long issuedMs, out string nonce)
    {
        userId = Guid.Empty; action = nonce = string.Empty; issuedMs = 0;

        var parts = token.Split('.');
        if (parts.Length != 2) return false;

        byte[] payloadBytes, sig;
        try
        {
            payloadBytes = FromB64Url(parts[0]);
            sig          = FromB64Url(parts[1]);
        }
        catch (FormatException) { return false; }

        var payload = Encoding.UTF8.GetString(payloadBytes);
        if (!CryptographicOperations.FixedTimeEquals(sig, Sign(payload))) return false;

        var fields = payload.Split('.');
        if (fields.Length != 4) return false;
        if (!Guid.TryParseExact(fields[0], "N", out userId)) return false;
        if (!long.TryParse(fields[2], out issuedMs)) return false;

        action = fields[1];
        nonce  = fields[3];
        return true;
    }

    /// <summary>
    /// 用戶端 IP：Azure Functions（App Service）前端會帶 X-Forwarded-For，取第一段並去掉 IPv4 的 :port；
    /// 沒有時退回連線位址（本機開發）。僅供稽核，**不可作為授權依據**（標頭可偽造）。
    /// </summary>
    private static string? ResolveClientIp(HttpRequest req)
    {
        var forwarded = req.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',')[0].Trim();
            var colon = first.LastIndexOf(':');
            if (colon > 0 && first.IndexOf(':') == colon) first = first[..colon];   // 1.2.3.4:5678
            return first;
        }
        return req.HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private byte[] Sign(string payload) => HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(payload));

    /// <summary>由 JWT 金鑰衍生專用金鑰，避免同一把金鑰跨用途簽章</summary>
    private static byte[] DeriveKey(string secret) =>
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes("attendance-clock-challenge"));

    private static string B64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromB64Url(string s)
    {
        var b = s.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(b.PadRight(b.Length + (4 - b.Length % 4) % 4, '='));
    }

    private static string? Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? null : s.Length <= max ? s : s[..max];
}
