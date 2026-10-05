namespace Jabez.Api.Common;

/// <summary>
/// 認證安全政策常數（登入鎖定 / Refresh Token 絕對期限 / 密碼強度）的單一真相。
/// 規格見 docs/authentication.md。
/// </summary>
public static class AuthPolicy
{
    /// <summary>登入連續失敗幾次後鎖定</summary>
    public const int MaxFailedLogins = 5;

    /// <summary>鎖定分鐘數；同時也是「連續失敗」的計數視窗長度</summary>
    public const int LockoutMinutes = 15;

    /// <summary>Refresh Token 輪替鏈的絕對有效天數（自原始登入起算，輪替不延長）</summary>
    public const int RefreshAbsoluteDays = 30;

    /// <summary>
    /// 已撤銷的 Refresh Token 在此秒數內再次出現，視為「多分頁同時 refresh 的競態」而非被盜用，
    /// 只回 401、不連坐撤銷該使用者全部 token。
    /// </summary>
    public const int RefreshReuseGraceSeconds = 30;

    /// <summary>新密碼最少碼數</summary>
    public const int PasswordMinLength = 8;

    /// <summary>JWT claim：此 token 的持有者尚未完成強制改密碼，API 僅放行改密碼 / refresh / logout</summary>
    public const string PasswordChangeRequiredClaim = "pwd_change_required";
}
