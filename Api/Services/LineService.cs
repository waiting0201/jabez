using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Jabez.Api.Models.Dtos;
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
    public async Task<PushResult> PushMessageAsync(string lineUserId, object messagePayload)
    {
        var jsonBody = JsonSerializer.Serialize(new { to = lineUserId, messages = new[] { messagePayload } });

        HttpResponseMessage resp;
        bool retried = false;
        try
        {
            resp = await SendPushAsync(jsonBody);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "LINE push 網路錯誤：UserId={UserId}", lineUserId);
            return new PushResult(false, null, "network_error", Truncate(ex.Message, 500));
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "LINE push timeout：UserId={UserId}", lineUserId);
            return new PushResult(false, null, "network_error", Truncate(ex.Message, 500));
        }

        // 429 Too Many Requests → 等 Retry-After（或預設 1 秒）後 retry 一次
        if (resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            var retryAfter = resp.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1);
            _logger.LogWarning(
                "LINE push 收到 429（rate limit），{Delay}s 後 retry：UserId={UserId}",
                retryAfter.TotalSeconds, lineUserId);
            resp.Dispose();
            await Task.Delay(retryAfter);
            try
            {
                resp = await SendPushAsync(jsonBody);
                retried = true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "LINE push retry 網路錯誤：UserId={UserId}", lineUserId);
                return new PushResult(false, null, "network_error", Truncate(ex.Message, 500));
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "LINE push retry timeout：UserId={UserId}", lineUserId);
                return new PushResult(false, null, "network_error", Truncate(ex.Message, 500));
            }
        }

        try
        {
            var status = (int)resp.StatusCode;
            if (resp.IsSuccessStatusCode) return new PushResult(true, status);

            var body = await resp.Content.ReadAsStringAsync();
            string category;

            // 401 / 403 → Token 過期或無效，整個推播管道將靜默失效，必須以 Critical 告警
            if (resp.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                category = "token_invalid";
                _logger.LogCritical(
                    "LINE Messaging Token 失效（{Status}）— 整個推播管道無法運作，請立即至 LINE Developers Console 重新發行 Token。Body={Body}",
                    resp.StatusCode, body);
            }
            // 400 且 body 提到未加好友 → 用戶層問題
            else if (body.Contains("hasn't added the LINE Official Account as a friend", StringComparison.OrdinalIgnoreCase)
                  || body.Contains("has been blocked by the user", StringComparison.OrdinalIgnoreCase))
            {
                category = "not_friend";
                _logger.LogError(
                    "LINE push 失敗（用戶未加 OA 好友或已封鎖）：UserId={UserId} Status={Status} Body={Body}",
                    lineUserId, resp.StatusCode, body);
            }
            // 429 retry 後仍失敗 → rate_limited
            else if (resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                category = "rate_limited";
                _logger.LogWarning(
                    "LINE push retry 後仍 429（rate limit 持續）：UserId={UserId} Body={Body}", lineUserId, body);
            }
            else
            {
                category = "unknown";
                _logger.LogWarning(
                    "LINE push failed to {UserId}: {Status} {Body} (retried={Retried})",
                    lineUserId, resp.StatusCode, body, retried);
            }
            return new PushResult(false, status, category, Truncate(body, 500));
        }
        finally
        {
            resp.Dispose();
        }
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max];

    /// <summary>共用單次 push 請求；caller 負責 dispose 回傳的 HttpResponseMessage。</summary>
    private async Task<HttpResponseMessage> SendPushAsync(string jsonBody)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.line.me/v2/bot/message/push")
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _messagingAccessToken);
        return await _http.SendAsync(request);
    }

    /// <inheritdoc />
    public async Task<LineQuotaDto?> GetMessageQuotaAsync()
    {
        // 並行打 quota + consumption（兩支獨立 API，互不依賴），降低 Dashboard 載入延遲
        var quotaTask       = SendQuotaRequestAsync("https://api.line.me/v2/bot/message/quota");
        var consumptionTask = SendQuotaRequestAsync("https://api.line.me/v2/bot/message/quota/consumption");

        try
        {
            await Task.WhenAll(quotaTask, consumptionTask);
        }
        catch
        {
            // 個別 task 的例外（network / timeout）已在 SendQuotaRequestAsync 內 log，這裡只需 fail-open 回 null
            return null;
        }

        var quotaJson       = quotaTask.Result;
        var consumptionJson = consumptionTask.Result;
        if (quotaJson is null || consumptionJson is null) return null;

        try
        {
            // quota: { "type": "limited" | "none", "value": <int> }
            var type = quotaJson.Value.GetProperty("type").GetString() ?? "none";
            int? limit = type == "limited" && quotaJson.Value.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.Number
                ? v.GetInt32()
                : null;

            // consumption: { "totalUsage": <int> }
            int used = consumptionJson.Value.TryGetProperty("totalUsage", out var u) && u.ValueKind == JsonValueKind.Number
                ? u.GetInt32()
                : 0;

            int? remaining = limit.HasValue ? Math.Max(0, limit.Value - used) : null;
            return new LineQuotaDto(type, limit, used, remaining);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LINE quota response 解析失敗");
            return null;
        }
    }

    /// <summary>共用 quota / consumption 取得邏輯。失敗時 log 並回 null（不丟例外）。</summary>
    private async Task<JsonElement?> SendQuotaRequestAsync(string url)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _messagingAccessToken);

            var resp = await _http.SendAsync(request);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _logger.LogWarning("LINE quota 查詢失敗：Url={Url} Status={Status} Body={Body}", url, resp.StatusCode, body);
                return null;
            }
            return await resp.Content.ReadFromJsonAsync<JsonElement>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LINE quota 查詢例外：Url={Url}", url);
            return null;
        }
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
