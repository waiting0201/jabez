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
    public async Task PushMessageAsync(string lineUserId, object messagePayload)
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
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            _logger.LogWarning("LINE push failed to {UserId}: {Status} {Body}", lineUserId, resp.StatusCode, body);
        }
    }
}
