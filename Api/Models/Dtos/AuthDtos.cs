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

/// <summary>登入時自動補打加班結束卡的結果資訊</summary>
public sealed record AutoOvertimeEndInfo(int Count, string[] Dates);
