using System.Data;
using System.Diagnostics;
using System.Globalization;
using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Services;

/// <summary>
/// 打卡提醒協調服務：判斷當前是否為上/下班前 N 分鐘的提醒時點，
/// 查詢符合條件的員工，並透過 LINE 推播 Flex Message。
/// 每次執行（不論 0 對象與否）都會寫入一筆 batchStart 紀錄，
/// 每筆推播後立即寫入 success/failure 紀錄（用 Dapper 直接 INSERT 避免 EF ChangeTracker 累積）。
/// </summary>
public sealed class AttendanceReminderService(
    AppDbContext db,
    IDbConnection conn,
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
        await PushAsync(type, now.Date, workTime, setting.SiteUrl, "auto", null, ct);
    }

    public async Task<AttendanceReminderRunResult> ForceRunAsync(string type, Guid? triggeredByUserId, CancellationToken ct = default)
    {
        if (type is not ("clockIn" or "clockOut"))
            throw AppException.BadRequest("type 必須為 clockIn 或 clockOut");

        var setting = await db.SystemSettings.AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.BadRequest("尚未設定 SystemSetting。");

        var workTime = type == "clockIn" ? setting.WorkStartTime : setting.WorkEndTime;
        return await PushAsync(type, Clock.Today, workTime, setting.SiteUrl, "manual", triggeredByUserId, ct);
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
        string type, DateTime today, string workTime, string siteUrl,
        string triggerSource, Guid? triggeredByUserId, CancellationToken ct)
    {
        // 將工作時間（"09:00"）與今日日期合成精確 targetTime，
        // 供 SQL 用「請假是否覆蓋此時刻」判斷（修正小時制請假被誤排除問題）。
        var targetTime = today.Date;
        if (TimeSpan.TryParseExact(workTime, [@"h\:mm", @"hh\:mm"], CultureInfo.InvariantCulture, out var ts))
            targetTime = today.Date.Add(ts);

        var batchId       = Guid.NewGuid();
        var tickedAtUtc   = DateTime.UtcNow;
        var tickedAtTaipei = Clock.Now;
        var targetTimeStr = workTime;  // "HH:mm"

        var recipients = await reader.GetRecipientsAsync(targetTime, type, ct);
        logger.LogInformation(
            "AttendanceReminder: type={Type} target={Target} recipientCount={Count} batchId={BatchId}",
            type, targetTime.ToString("yyyy-MM-dd HH:mm"), recipients.Count, batchId);

        // 寫一筆 batchStart：即使 0 對象也能驗證排程有跑、命中時點
        await SafeWriteLogAsync(new AttendanceReminderLogRow(
            BatchId: batchId,
            TickedAt: tickedAtUtc,
            TickedAtTaipei: tickedAtTaipei,
            TargetTimeTaipei: targetTimeStr,
            ReminderType: "batchStart",
            TriggerSource: triggerSource,
            TriggeredByUserId: triggeredByUserId,
            UserId: null,
            LineUserIdSnapshot: null,
            UserNameSnapshot: $"recipientCount={recipients.Count}",
            Status: "batchStart",
            ErrorCategory: null,
            ErrorMessage: null,
            HttpStatusCode: null,
            DurationMs: null), ct);

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

            var sw = Stopwatch.StartNew();
            PushResult pr;
            try
            {
                var flex = LineFlexMessageBuilder.BuildAttendanceReminderMessage(
                    type, r.UserName, LeadMinutes, workTime, linkUrl);
                pr = await lineService.PushMessageAsync(r.LineUserId, flex);
                if (pr.Success) pushed++;
                else            failed++;
            }
            catch (Exception ex)
            {
                failed++;
                pr = new PushResult(false, null, "system_error", Truncate(ex.Message, 500));
                // 能進入 catch 代表是 lineService 簽章外的非預期例外。
                // 升級為 Error 以利監控告警；單一員工失敗不阻斷其他人推播。
                logger.LogError(ex,
                    "打卡提醒推播例外（系統錯誤）：UserId={UserId}, Name={Name}", r.UserId, r.UserName);
            }
            finally
            {
                sw.Stop();
            }

            // 寫 success / failure 紀錄（Dapper INSERT，不影響推播主流程）
            await SafeWriteLogAsync(new AttendanceReminderLogRow(
                BatchId: batchId,
                TickedAt: tickedAtUtc,
                TickedAtTaipei: tickedAtTaipei,
                TargetTimeTaipei: targetTimeStr,
                ReminderType: type,
                TriggerSource: triggerSource,
                TriggeredByUserId: triggeredByUserId,
                UserId: r.UserId,
                LineUserIdSnapshot: r.LineUserId,
                UserNameSnapshot: r.UserName,
                Status: pr.Success ? "success" : "failure",
                ErrorCategory: pr.ErrorCategory,
                ErrorMessage: pr.ErrorMessage,
                HttpStatusCode: pr.HttpStatusCode,
                DurationMs: (int)sw.ElapsedMilliseconds), ct);
        }

        return new AttendanceReminderRunResult(recipients.Count, pushed, failed, batchId);
    }

    /// <summary>
    /// 寫推播紀錄至 AttendanceReminderLogs。失敗只記 log，絕不 throw — 寫紀錄失敗不能影響推播主流程。
    /// 用 Dapper INSERT 避開 EF ChangeTracker 在迴圈中的累積污染。
    /// </summary>
    private async Task SafeWriteLogAsync(AttendanceReminderLogRow row, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO AttendanceReminderLogs
                (BatchId, TickedAt, TickedAtTaipei, TargetTimeTaipei, ReminderType,
                 TriggerSource, TriggeredByUserId, UserId, LineUserIdSnapshot, UserNameSnapshot,
                 Status, ErrorCategory, ErrorMessage, HttpStatusCode, DurationMs, CreatedAt)
            VALUES
                (@BatchId, @TickedAt, @TickedAtTaipei, @TargetTimeTaipei, @ReminderType,
                 @TriggerSource, @TriggeredByUserId, @UserId, @LineUserIdSnapshot, @UserNameSnapshot,
                 @Status, @ErrorCategory, @ErrorMessage, @HttpStatusCode, @DurationMs, GETUTCDATE());
            """;
        try
        {
            var cmd = new CommandDefinition(sql, row, cancellationToken: ct);
            await conn.ExecuteAsync(cmd);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "AttendanceReminderLog 寫入失敗：BatchId={BatchId} UserId={UserId} Status={Status}",
                row.BatchId, row.UserId, row.Status);
        }
    }

    private static string? Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);

    /// <summary>內部用 row 結構，欄位名與 SQL 參數一一對應。</summary>
    private sealed record AttendanceReminderLogRow(
        Guid     BatchId,
        DateTime TickedAt,
        DateTime TickedAtTaipei,
        string   TargetTimeTaipei,
        string   ReminderType,
        string   TriggerSource,
        Guid?    TriggeredByUserId,
        Guid?    UserId,
        string?  LineUserIdSnapshot,
        string?  UserNameSnapshot,
        string   Status,
        string?  ErrorCategory,
        string?  ErrorMessage,
        int?     HttpStatusCode,
        int?     DurationMs);
}
