namespace Jabez.Api.Services;

/// <summary>
/// Cloudflare Turnstile 人機驗證（防機器人打卡第四道關卡，2026-10）。
/// 前端以 widget 取得一次性 token，後端向 Cloudflare siteverify 驗證。
/// </summary>
public interface ITurnstileVerifier
{
    /// <summary>目前模式（<see cref="TurnstileModes"/>）；未設定時為 off</summary>
    string Mode { get; }

    /// <summary>
    /// 驗證 token，回傳結果代碼（<see cref="TurnstileResults"/>）。
    /// 不丟例外：連線失敗 / 逾時一律回 unavailable，由呼叫端決定放行與否。
    /// </summary>
    Task<string> VerifyAsync(string? token, string expectedAction, string? remoteIp, CancellationToken ct = default);
}

/// <summary>設定 <c>Turnstile:Mode</c> 的三種值</summary>
public static class TurnstileModes
{
    /// <summary>不驗證（預設；本機開發 / 尚未設定 secret 時）</summary>
    public const string Off     = "off";
    /// <summary>驗證並記錄結果，但一律放行（上線觀察期）</summary>
    public const string Log     = "log";
    /// <summary>missing / failed 擋下</summary>
    public const string Enforce = "enforce";
}

/// <summary>驗證結果代碼（寫入 AttendancePunchLog.TurnstileResult）</summary>
public static class TurnstileResults
{
    public const string Ok          = "ok";
    public const string Missing     = "missing";
    public const string Failed      = "failed";
    /// <summary>Cloudflare 連不到 / 逾時：一律放行（fail-open），見 AttendancePunchGuard</summary>
    public const string Unavailable = "unavailable";
    /// <summary>模式為 off，未驗證</summary>
    public const string Skipped     = "skipped";
}
