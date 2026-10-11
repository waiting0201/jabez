using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Services;

/// <summary>
/// Cloudflare Turnstile siteverify 包裝。文件：https://developers.cloudflare.com/turnstile/get-started/server-side-validation/
/// 由 IHttpClientFactory 注入 HttpClient（已設定 BaseAddress 與 Timeout）。
///
/// 設定鍵（Azure App Setting 以 <c>__</c> 取代 <c>:</c>）：
///   <c>Turnstile:SecretKey</c>        — 只放後端，絕不可進前端 / 版控
///   <c>Turnstile:Mode</c>             — off（預設）/ log / enforce
///   <c>Turnstile:AllowedHostnames</c> — 逗號分隔；空白＝不檢查 hostname
///
/// 除了 <c>success</c> 之外另比對回應的 <c>action</c> 與 <c>hostname</c>：
/// 否則別的網站（或同站別的 widget）取得的 token 也能拿來打卡。
/// </summary>
public sealed class TurnstileVerifier(HttpClient http, IConfiguration config, ILogger<TurnstileVerifier> logger)
    : ITurnstileVerifier
{
    private readonly string? _secret = config["Turnstile:SecretKey"];

    private readonly HashSet<string> _hostnames = new(
        (config["Turnstile:AllowedHostnames"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        StringComparer.OrdinalIgnoreCase);

    public string Mode { get; } = NormalizeMode(config["Turnstile:Mode"], config["Turnstile:SecretKey"]);

    /// <summary>
    /// Cloudflare 官方測試 secret（1x… 一律通過 / 2x… 一律失敗 / 3x… token 已用過）。
    /// 測試金鑰的回應**不帶 action、hostname 固定為 example.com**，比對必然失敗，故僅在此情況跳過兩項比對；
    /// 判斷依據是「我們自己設定的 secret」而非回應內容，正式 secret 不受影響。
    /// </summary>
    private bool IsTestingSecret => _secret is not null && _secret.Length > 2 && _secret[1] == 'x' && _secret[2..].All(c => c == '0' || c == 'A');

    public async Task<string> VerifyAsync(string? token, string expectedAction, string? remoteIp, CancellationToken ct = default)
    {
        if (Mode == TurnstileModes.Off) return TurnstileResults.Skipped;
        if (string.IsNullOrWhiteSpace(token) || token.Length > 2048) return TurnstileResults.Missing;

        var form = new Dictionary<string, string> { ["secret"] = _secret!, ["response"] = token };
        if (!string.IsNullOrWhiteSpace(remoteIp)) form["remoteip"] = remoteIp;

        try
        {
            using var response = await http.PostAsync("turnstile/v0/siteverify", new FormUrlEncodedContent(form), ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Turnstile siteverify non-success status: {Status}", response.StatusCode);
                return TurnstileResults.Unavailable;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            if (!root.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True)
            {
                logger.LogInformation("Turnstile verify failed: {Errors}",
                    root.TryGetProperty("error-codes", out var errors) ? errors.ToString() : "-");
                return TurnstileResults.Failed;
            }

            if (IsTestingSecret) return TurnstileResults.Ok;

            var action   = GetString(root, "action");
            var hostname = GetString(root, "hostname");

            if (!string.Equals(action, expectedAction, StringComparison.Ordinal))
            {
                logger.LogInformation("Turnstile action mismatch: expected {Expected}, got {Actual}", expectedAction, action);
                return TurnstileResults.Failed;
            }
            if (_hostnames.Count > 0 && (hostname is null || !_hostnames.Contains(hostname)))
            {
                logger.LogInformation("Turnstile hostname not allowed: {Hostname}", hostname);
                return TurnstileResults.Failed;
            }

            return TurnstileResults.Ok;
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("Turnstile siteverify timeout");
            return TurnstileResults.Unavailable;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Turnstile siteverify call failed");
            return TurnstileResults.Unavailable;
        }
    }

    /// <summary>未知值退回 off；沒有 secret 時強制 off（設了 enforce 卻忘了 secret 會讓全公司打不了卡）</summary>
    private static string NormalizeMode(string? mode, string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret)) return TurnstileModes.Off;
        return mode?.Trim().ToLowerInvariant() switch
        {
            TurnstileModes.Log     => TurnstileModes.Log,
            TurnstileModes.Enforce => TurnstileModes.Enforce,
            _                      => TurnstileModes.Off,
        };
    }

    private static string? GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
