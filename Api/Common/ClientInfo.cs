using Microsoft.AspNetCore.Http;

namespace Jabez.Api.Common;

/// <summary>
/// 取得呼叫端的 IP / User-Agent（稽核用，非授權依據）。
/// IP 寫法與 AttendancePunchGuard.ResolveClientIp 一致：Azure 前端代理放在 X-Forwarded-For，取第一段並去掉 port。
/// 注意 X-Forwarded-For 可被呼叫端偽造，只能當稽核線索，不可當鎖定 / 授權的鍵。
/// </summary>
public static class ClientInfo
{
    public static string? ResolveIp(HttpRequest req, int maxLength = 64)
    {
        string? ip;
        var forwarded = req.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',')[0].Trim();
            var colon = first.LastIndexOf(':');
            if (colon > 0 && first.IndexOf(':') == colon) first = first[..colon];   // 1.2.3.4:5678
            ip = first;
        }
        else
        {
            ip = req.HttpContext.Connection.RemoteIpAddress?.ToString();
        }
        return Truncate(ip, maxLength);
    }

    public static string? ResolveUserAgent(HttpRequest req, int maxLength = 512)
        => Truncate(req.Headers.UserAgent.ToString(), maxLength);

    private static string? Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? null : (s.Length <= max ? s : s[..max]);
}
