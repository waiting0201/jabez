namespace Jabez.Api.Models.Entities;

/// <summary>
/// 登入嘗試紀錄（2026-10 帳號安全強化）：<c>POST /auth/login</c> 的**每一次嘗試**都留一列，
/// 成功、密碼錯誤、帳號不存在、帳號停用、鎖定中擋下皆記錄，供鎖定判定與事後稽核（IP / User-Agent）。
/// 鎖定判定規則見 <see cref="Jabez.Api.Services.LoginAttemptTracker"/>。
/// <para>
/// <c>UserId</c> 刻意**不建外鍵**：帳號不存在的嘗試本來就沒有 UserId，
/// 且使用者被硬刪除後稽核紀錄應保留（也就不必加進 UserHandler 的 NO_ACTION 清洗清單）。
/// </para>
/// </summary>
public class LoginAttempt
{
    public long      Id            { get; set; }
    /// <summary>輸入的 Email（trim + 小寫正規化，截至 256 字）；鎖定以此為鍵，故帳號不存在時同樣會鎖，無法由鎖定與否列舉帳號</summary>
    public string    Email         { get; set; } = string.Empty;
    public Guid?     UserId        { get; set; }
    public bool      Succeeded     { get; set; }
    /// <summary>失敗原因代碼（見 <see cref="LoginFailureReasons"/>），成功為 null</summary>
    public string?   FailureReason { get; set; }
    public string?   IpAddress     { get; set; }
    public string?   UserAgent     { get; set; }
    public DateTime  AttemptedAt   { get; set; }
}

public static class LoginFailureReasons
{
    /// <summary>密碼錯誤（計入鎖定）</summary>
    public const string BadPassword  = "bad_password";
    /// <summary>帳號不存在（計入鎖定，使其與「密碼錯誤」不可區分）</summary>
    public const string UnknownEmail = "unknown_email";
    /// <summary>密碼正確但帳號已停用（不計入鎖定）</summary>
    public const string Inactive     = "inactive";
    /// <summary>鎖定中被擋下（不計入鎖定，避免鎖定被無限延長）</summary>
    public const string Locked       = "locked";
}
