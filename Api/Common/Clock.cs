namespace Jabez.Api.Common;

/// <summary>
/// 統一時區工具：所有使用者可見的時間戳記皆使用台北時區（UTC+8）
/// </summary>
public static class Clock
{
    private static readonly TimeZoneInfo TaipeiTz =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

    /// <summary>取得台北時區的當前時間</summary>
    public static DateTime Now =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaipeiTz);

    /// <summary>取得台北時區的今日日期</summary>
    public static DateTime Today => Now.Date;
}
