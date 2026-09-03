namespace Jabez.Api.Models.Dtos;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, string TokenType = "Bearer");

public sealed record RefreshRequest(string RefreshToken);

public sealed record RefreshResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType = "Bearer");

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>登入時自動補打卡的結果資訊</summary>
public sealed record AutoClockOutInfo(int Count, string[] Dates);

/// <summary>登入時自動補打上班卡的結果資訊</summary>
public sealed record AutoClockInInfo(int Count, string[] Dates);

/// <summary>登入時自動補打加班結束卡的結果資訊</summary>
public sealed record AutoOvertimeEndInfo(int Count, string[] Dates);

/// <summary>
/// 登入自動補卡的整體結果（三種缺口各一份，無補卡者為 null）。
/// 見 <see cref="Jabez.Api.Services.AttendanceAutoClockService"/>。
/// </summary>
public sealed record AutoClockResult(
    AutoClockInInfo?     ClockIn,
    AutoClockOutInfo?    ClockOut,
    AutoOvertimeEndInfo? OvertimeEnd)
{
    public static readonly AutoClockResult Empty = new(null, null, null);
}
