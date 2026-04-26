using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Services;

/// <summary>LINE Platform REST API 封裝（HttpClient 注入）。</summary>
public sealed class LineService : ILineService
{
    private readonly HttpClient _http;
    private readonly ILogger<LineService> _logger;
    private readonly string _loginChannelId;
    private readonly string _loginChannelSecret;
    private readonly string _messagingAccessToken;

    public LineService(HttpClient http, IConfiguration cfg, ILogger<LineService> logger)
    {
        _http = http;
        _logger = logger;
        _loginChannelId      = cfg["Line:LoginChannelId"] ?? "";
        _loginChannelSecret  = cfg["Line:LoginChannelSecret"] ?? "";
        _messagingAccessToken = cfg["Line:MessagingChannelAccessToken"] ?? "";
    }

    /// <inheritdoc />
    public async Task<string?> ExchangeCodeForUserIdAsync(string code, string redirectUri)
    {
        // Step 1: 用 code 換取 token（含 id_token）
        var form = new Dictionary<string, string>
        {
            ["grant_type"]    = "authorization_code",
            ["code"]          = code,
            ["redirect_uri"]  = redirectUri,
            ["client_id"]     = _loginChannelId,
            ["client_secret"] = _loginChannelSecret,
        };

        var resp = await _http.PostAsync(
            "https://api.line.me/oauth2/v2.1/token",
            new FormUrlEncodedContent(form));

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            _logger.LogWarning("LINE token exchange failed: {Status} {Body}", resp.StatusCode, body);
            return null;
        }

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var idToken = json.GetProperty("id_token").GetString();
        if (string.IsNullOrEmpty(idToken))
        {
            _logger.LogWarning("LINE token response missing id_token");
            return null;
        }

        // Step 2: 解析 id_token 取得 sub（LINE userId）
        // id_token 由 LINE 簽發，已經過 LINE 伺服器驗證（我們用 code 換取的）
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(idToken);
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (string.IsNullOrEmpty(sub))
        {
            _logger.LogWarning("LINE id_token missing sub claim");
            return null;
        }

        return sub;
    }

    /// <inheritdoc />
    public async Task<bool> PushMessageAsync(string lineUserId, object messagePayload)
    {
        var payload = new { to = lineUserId, messages = new[] { messagePayload } };
        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.line.me/v2/bot/message/push")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _messagingAccessToken);

        var resp = await _http.SendAsync(request);
        if (resp.IsSuccessStatusCode)
            return true;

        var body = await resp.Content.ReadAsStringAsync();
        // 401 / 403 → Token 過期或無效，整個推播管道將靜默失效，必須以 Critical 告警
        if (resp.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogCritical(
                "LINE Messaging Token 失效（{Status}）— 整個推播管道無法運作，請立即至 LINE Developers Console 重新發行 Token。Body={Body}",
                resp.StatusCode, body);
        }
        // 400 且 body 提到未加好友 → 用戶層問題，升級為 Error 清楚標示
        else if (body.Contains("hasn't added the LINE Official Account as a friend", StringComparison.OrdinalIgnoreCase)
              || body.Contains("has been blocked by the user", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "LINE push 失敗（用戶未加 OA 好友或已封鎖）：UserId={UserId} Status={Status} Body={Body}",
                lineUserId, resp.StatusCode, body);
        }
        else
        {
            _logger.LogWarning("LINE push failed to {UserId}: {Status} {Body}", lineUserId, resp.StatusCode, body);
        }
        return false;
    }

    /// <inheritdoc />
    public async Task<bool> IsBotFriendAsync(string lineUserId)
    {
        if (string.IsNullOrEmpty(lineUserId)) return false;
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.line.me/v2/bot/profile/{Uri.EscapeDataString(lineUserId)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _messagingAccessToken);

            var resp = await _http.SendAsync(request);
            if (resp.IsSuccessStatusCode) return true;

            // 404 = 非好友或已封鎖；其他非 200 視為不可用，記 warning
            if ((int)resp.StatusCode != 404)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "LINE profile 查詢失敗：UserId={UserId} Status={Status} Body={Body}",
                    lineUserId, resp.StatusCode, body);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LINE profile 查詢例外：UserId={UserId}", lineUserId);
            return false;
        }
    }
}
