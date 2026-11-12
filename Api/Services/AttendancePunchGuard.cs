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
/// 四道關卡，四個本人打卡動作（上班 / 下班 / 加班開始 / 加班結束）共用：
///   1. **強制 GPS**：座標缺漏、超出範圍或為 (0,0) 一律拒絕。2026-10-11 曾改為選填（地下室等收不到定位處的同仁
///      會完全無法打卡），2026-11 依公司政策恢復必填；前端取定位失敗會先降級改用 Wi-Fi／基地台定位重試一次。
///   2. **Cloudflare Turnstile 人機驗證**（2026-10 新增，<see cref="ITurnstileVerifier"/>）：
///      依設定 <c>Turnstile:Mode</c> —— off 不驗證、log 只記錄結果一律放行（上線觀察期）、enforce 擋下 missing / failed。
///      **Cloudflare 連不到（unavailable）一律放行並記錄**：不能因外部服務故障讓全公司打不了卡，其餘關卡仍有效。
///   3. **一次性挑戰碼**：打卡前須先呼叫 POST /attendances/clock-challenge 取碼，
///      碼以 HMAC 簽章綁定「使用者 + 動作 + 簽發時間 + nonce」，
///      **簽發後至少 <see cref="MinAgeMs"/> 毫秒、至多 <see cref="MaxAgeMs"/> 毫秒內**才能使用，且只能成功使用一次。
///      前端在取得 GPS 的同時取碼、不足最短時間時自動補等，正常使用者不會察覺。
///   4. **嘗試紀錄**：成功與被擋下的每一次嘗試都寫入 AttendancePunchLogs（IP / User-Agent / GPS / 停留時間 / Turnstile 結果）。
///      **通過關卡後才被業務檢查擋下的嘗試也留一列**（2026-11，<see cref="RecordRejectionAsync"/>，原因代碼 business_rule ＋ 當下回給使用者的訊息）——
///      原本這類嘗試完全不留痕跡，員工回報「我明明有打卡」時查不出是沒送出、還是送出後被哪一條規則擋下。
///
/// ⚠ GPS 與挑戰碼只是「提高門檻 + 留證據」：改寫過的腳本仍可模仿取碼與等待。
///   Turnstile 要求真的跑過 Cloudflare 的瀏覽器挑戰，腳本成本大幅提高；但仍擋不住「把手機交給同事代打」。
/// 系統自動補卡（AttendanceAutoClockService）與管理者修改（PUT /attendances/{id}）不經此流程。
/// </summary>
public sealed class AttendancePunchGuard(AppDbContext db, IConfiguration config, ITurnstileVerifier turnstile)
    : IAttendancePunchGuard
{
    /// <summary>挑戰碼最短停留時間：腳本「取碼後立刻打卡」會被擋</summary>
    public const int MinAgeMs = 3000;

    /// <summary>挑戰碼有效時間</summary>
    public const int MaxAgeMs = 5 * 60 * 1000;

    /// <summary>BlockMessage 欄位長度（與 AttendancePunchLogConfiguration 同步）</summary>
    public const int BlockMessageMaxLength = 200;

    /// <summary>可打卡的四個動作（與路由 /attendances/{action} 同名）</summary>
    public static readonly HashSet<string> Actions = ["clock-in", "clock-out", "overtime-start", "overtime-end"];

    /// <summary>擋下原因代碼（寫入 AttendancePunchLog.BlockReason）</summary>
    public static class BlockReasons
    {
        public const string NoGps            = "no_gps";
        public const string ChallengeMissing = "challenge_missing";
        public const string ChallengeInvalid = "challenge_invalid";
        public const string TooFast          = "too_fast";
        public const string ChallengeExpired = "challenge_expired";
        public const string ChallengeReused  = "challenge_reused";
        public const string TurnstileMissing = "turnstile_missing";
        public const string TurnstileFailed  = "turnstile_failed";
        /// <summary>四道關卡都通過，但被打卡的業務檢查擋下（例：未填逾時原因、專案佔比合計不是 100%）；訊息見 BlockMessage</summary>
        public const string BusinessRule     = "business_rule";
        /// <summary>四道關卡都通過，但寫入時發生非預期錯誤（500）</summary>
        public const string ServerError      = "server_error";
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

    /// <summary>本次請求已通過關卡、尚未落地的成功紀錄；業務檢查失敗時由 <see cref="RecordRejectionAsync"/> 改寫成失敗紀錄</summary>
    private AttendancePunchLog? _pending;

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

        var (reason, message) = await EvaluateAsync(userId, action, body, log, log.IpAddress);

        log.Succeeded    = reason is null;
        log.BlockReason  = reason;
        log.BlockMessage = Truncate(message, BlockMessageMaxLength);
        db.AttendancePunchLogs.Add(log);

        if (reason is null)
        {
            _pending = log;
            return;
        }

        await db.SaveChangesAsync();
        throw AppException.BadRequest(message!);
    }

    public async Task RecordRejectionAsync(Exception ex)
    {
        if (_pending is not { } log) return;   // 關卡沒過（已自行落地）或根本還沒走到關卡
        _pending = null;

        // 已隨打卡紀錄落地（Id 已產生）＝打卡本身成功，是之後的步驟（組回應等）出錯，不可改記成失敗
        if (log.Id != 0) return;

        try
        {
            // 請求已確定失敗：丟掉 DbContext 裡所有未落地的變更（含半途改過的打卡紀錄），只單獨寫入這一筆嘗試紀錄
            db.ChangeTracker.Clear();

            log.Succeeded   = false;   // 挑戰碼不視為已使用（filtered unique index 只約束成功列）
            log.BlockReason = ex is AppException ? BlockReasons.BusinessRule : BlockReasons.ServerError;
            log.BlockMessage = Truncate(ex is AppException ? ex.Message : ex.GetType().Name, BlockMessageMaxLength);

            db.AttendancePunchLogs.Add(log);
            await db.SaveChangesAsync();
        }
        catch
        {
            // 紀錄寫不進去不可蓋掉原本的錯誤：呼叫端會把原例外繼續往外丟
        }
    }

    private async Task<(string? Reason, string? Message)> EvaluateAsync(
        Guid userId, string action, ClockActionRequest body, AttendancePunchLog log, string? remoteIp)
    {
        // 1. 強制 GPS（2026-11 依公司政策恢復；10/11–11/2 期間曾為選填）
        if (body.Latitude is not { } lat || body.Longitude is not { } lng
            || double.IsNaN(lat) || double.IsNaN(lng)
            || lat is < -90 or > 90 || lng is < -180 or > 180
            || (lat == 0 && lng == 0))
            return (BlockReasons.NoGps, "無法取得定位，請開啟手機／瀏覽器的定位權限後再打卡。");

        // 2. Turnstile 人機驗證（unavailable 一律放行，見類別說明）
        log.TurnstileResult = await turnstile.VerifyAsync(body.TurnstileToken, action, remoteIp);
        if (turnstile.Mode == TurnstileModes.Enforce)
        {
            if (log.TurnstileResult == TurnstileResults.Missing)
                return (BlockReasons.TurnstileMissing, "人機驗證失敗，請重新整理頁面後再試。");
            if (log.TurnstileResult == TurnstileResults.Failed)
                return (BlockReasons.TurnstileFailed, "人機驗證失敗，請重新整理頁面後再試。");
        }

        // 3. 挑戰碼
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
