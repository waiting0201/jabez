using System.Globalization;
using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Services;

/// <summary>
/// 打卡提醒協調服務：判斷當前是否為上/下班前 N 分鐘的提醒時點，
/// 查詢符合條件的員工，並透過 LINE 推播 Flex Message。
/// </summary>
public sealed class AttendanceReminderService(
    AppDbContext db,
    IAttendanceReminderReadService reader,
    ILineService lineService,
    ILogger<AttendanceReminderService> logger) : IAttendanceReminderService
{
    /// <summary>提醒提前時間（分鐘）。</summary>
    private const int LeadMinutes = 2;

    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = Clock.Now;

        // 週末不提醒
        if (now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return;

        var setting = await db.SystemSettings.AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(ct);
        if (setting is null)
            return;

        var type = DetermineReminderType(now, setting.WorkStartTime, setting.WorkEndTime);
        if (type is null)
            return;

        var workTime = type == "clockIn" ? setting.WorkStartTime : setting.WorkEndTime;
        await PushAsync(type, now.Date, workTime, setting.SiteUrl, ct);
    }

    public async Task<int> ForceRunAsync(string type, CancellationToken ct = default)
    {
        if (type is not ("clockIn" or "clockOut"))
            throw AppException.BadRequest("type 必須為 clockIn 或 clockOut");

        var setting = await db.SystemSettings.AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.BadRequest("尚未設定 SystemSetting。");

        var workTime = type == "clockIn" ? setting.WorkStartTime : setting.WorkEndTime;
        return await PushAsync(type, Clock.Today, workTime, setting.SiteUrl, ct);
    }

    /// <summary>
    /// 比對台北時間 HH:mm 是否命中「上/下班時刻 - LeadMinutes」。
    /// 以字串等值比對，精確到分；命中則回傳 "clockIn" / "clockOut"，否則 null。
    /// </summary>
    private static string? DetermineReminderType(DateTime taipeiNow, string workStart, string workEnd)
    {
        var targetIn  = SubtractMinutesHHmm(workStart, LeadMinutes);
        var targetOut = SubtractMinutesHHmm(workEnd,   LeadMinutes);
        var current   = taipeiNow.ToString("HH:mm", CultureInfo.InvariantCulture);

        if (targetIn  is not null && current == targetIn)  return "clockIn";
        if (targetOut is not null && current == targetOut) return "clockOut";
        return null;
    }

    /// <summary>將 "HH:mm" 減去指定分鐘數，回傳新的 "HH:mm"；跨日時回繞。</summary>
    private static string? SubtractMinutesHHmm(string hhmm, int minutes)
    {
        if (string.IsNullOrWhiteSpace(hhmm))
            return null;

        if (!TimeSpan.TryParseExact(hhmm, @"h\:mm", CultureInfo.InvariantCulture, out var t)
         && !TimeSpan.TryParseExact(hhmm, @"hh\:mm", CultureInfo.InvariantCulture, out t))
            return null;

        var shifted = t.Subtract(TimeSpan.FromMinutes(minutes));
        if (shifted < TimeSpan.Zero)
            shifted = shifted.Add(TimeSpan.FromDays(1));
        return $"{shifted.Hours:D2}:{shifted.Minutes:D2}";
    }

    private async Task<int> PushAsync(
        string type, DateTime today, string workTime, string siteUrl, CancellationToken ct)
    {
        var recipients = await reader.GetRecipientsAsync(today, type, ct);
        logger.LogInformation(
            "AttendanceReminder: type={Type} date={Date} recipientCount={Count}",
            type, today.ToString("yyyy-MM-dd"), recipients.Count);

        var linkUrl = $"{siteUrl.TrimEnd('/')}/dashboard";

        foreach (var r in recipients)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var flex = LineFlexMessageBuilder.BuildAttendanceReminderMessage(
                    type, r.UserName, LeadMinutes, workTime, linkUrl);
                await lineService.PushMessageAsync(r.LineUserId, flex);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "打卡提醒推播失敗：UserId={UserId}, Name={Name}", r.UserId, r.UserName);
            }
        }

        return recipients.Count;
    }
}
