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

    /// <summary>推播間隔（毫秒）：避免一次性 burst 觸發 LINE 速率限制。</summary>
    private const int InterPushDelayMs = 100;

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

    public async Task<AttendanceReminderRunResult> ForceRunAsync(string type, CancellationToken ct = default)
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

    private async Task<AttendanceReminderRunResult> PushAsync(
        string type, DateTime today, string workTime, string siteUrl, CancellationToken ct)
    {
        // 將工作時間（"09:00"）與今日日期合成精確 targetTime，
        // 供 SQL 用「請假是否覆蓋此時刻」判斷（修正小時制請假被誤排除問題）。
        var targetTime = today.Date;
        if (TimeSpan.TryParseExact(workTime, [@"h\:mm", @"hh\:mm"], CultureInfo.InvariantCulture, out var ts))
            targetTime = today.Date.Add(ts);

        var recipients = await reader.GetRecipientsAsync(targetTime, type, ct);
        logger.LogInformation(
            "AttendanceReminder: type={Type} target={Target} recipientCount={Count}",
            type, targetTime.ToString("yyyy-MM-dd HH:mm"), recipients.Count);

        var linkUrl = $"{siteUrl.TrimEnd('/')}/dashboard";

        int pushed = 0;
        int failed = 0;
        for (int i = 0; i < recipients.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var r = recipients[i];

            // 從第二個開始加入間隔，避免一次性 burst 觸發 429（PushMessageAsync 仍會 retry 一次）
            if (i > 0)
                await Task.Delay(InterPushDelayMs, ct);

            try
            {
                var flex = LineFlexMessageBuilder.BuildAttendanceReminderMessage(
                    type, r.UserName, LeadMinutes, workTime, linkUrl);
                if (await lineService.PushMessageAsync(r.LineUserId, flex))
                    pushed++;
                else
                    failed++;   // LineService 內已寫 log（未加好友 / token 失效 / 其他錯誤）
            }
            catch (Exception ex)
            {
                failed++;
                // 能進入 catch 代表是網路/系統層級例外（HttpRequestException、Timeout 等）。
                // 升級為 Error 以利監控告警；單一員工失敗不阻斷其他人推播。
                logger.LogError(ex,
                    "打卡提醒推播例外（系統錯誤）：UserId={UserId}, Name={Name}", r.UserId, r.UserName);
            }
        }

        return new AttendanceReminderRunResult(recipients.Count, pushed, failed);
    }
}
