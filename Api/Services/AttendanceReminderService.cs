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

    /// <summary>
    /// 命中時間窗（分鐘）：目標時刻起算 N 分鐘內的任何一次 tick 都算命中。
    ///
    /// 原本是「HH:mm 精確等值」，只要那一分鐘的 tick 沒跑到就整天不發 —— Flex Consumption
    /// 冷啟動經常把 tick 延遲數十秒到數分鐘，正式站 2026-07-06 與 2026-08-06 的上班提醒
    /// 就是這樣整天靜默。放寬成時間窗後，窗內會有多個 tick 命中，再由
    /// <see cref="HasBatchStartedTodayAsync"/> 收斂成一天一次。
    /// </summary>
    private const int WindowMinutes = 10;

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

        // 冪等閘：同一槽今天已推過就不再推。擋掉兩種重複來源 ——
        //   (1) 時間窗內的後續 tick（放寬窗口的必要配套）
        //   (2) 同一個 occurrence 被多個實例各跑一次：正式站 2026-07-13 出現
        //       08:58 / 08:59 兩個 BatchId、員工收到兩則重複推播，兩次相隔約 60 秒，
        //       足夠讓後者看見前者寫下的 batchStart。
        if (await HasBatchStartedTodayAsync(now.Date, workTime, ct))
            return;

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
    /// 判斷台北時間是否落在上/下班的提醒時間窗內。
    /// 窗 = [上/下班時刻 − LeadMinutes, + WindowMinutes)；命中回 "clockIn" / "clockOut"，否則 null。
    /// 上班窗優先判斷（正常設定下兩窗不會重疊）。
    /// </summary>
    private static string? DetermineReminderType(DateTime taipeiNow, string workStart, string workEnd)
    {
        if (IsWithinWindow(taipeiNow, workStart)) return "clockIn";
        if (IsWithinWindow(taipeiNow, workEnd))   return "clockOut";
        return null;
    }

    /// <summary>台北時間是否落在「workTime − LeadMinutes」起算的 WindowMinutes 分鐘窗內；跨午夜會正確回繞。</summary>
    private static bool IsWithinWindow(DateTime taipeiNow, string workTime)
    {
        if (!TryParseHHmm(workTime, out var work))
            return false;

        const int minutesPerDay = 24 * 60;
        var nowMin   = (taipeiNow.Hour * 60) + taipeiNow.Minute;
        var startMin = (((int)work.TotalMinutes - LeadMinutes) + minutesPerDay) % minutesPerDay;
        var endMin   = startMin + WindowMinutes;

        return endMin <= minutesPerDay
            ? nowMin >= startMin && nowMin < endMin
            : nowMin >= startMin || nowMin < endMin - minutesPerDay;   // 窗跨過午夜
    }

    /// <summary>解析 SystemSetting 的 "HH:mm"（容忍 "H:mm"）；格式不合回 false。</summary>
    private static bool TryParseHHmm(string hhmm, out TimeSpan value)
    {
        value = default;
        return !string.IsNullOrWhiteSpace(hhmm)
            && TimeSpan.TryParseExact(hhmm, [@"h\:mm", @"hh\:mm"], CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// 今天這一槽是否已寫過 batchStart（＝已推播過，不論成功失敗）。
    /// 以 TargetTimeTaipei 區分上/下班兩槽（上班存 WorkStartTime、下班存 WorkEndTime）；
    /// 兩者設成相同時間屬不合理設定，會被視為同一槽而只發一次。
    /// 手動觸發（ForceRunAsync）也會寫 batchStart，因此當天手動推過之後排程就不再重複打擾員工。
    /// 查詢失敗一律回 false —— 寧可重複推播，也不要因為 log 表出狀況而整天不發。
    /// </summary>
    private async Task<bool> HasBatchStartedTodayAsync(DateTime today, string workTime, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 1 1
            FROM   AttendanceReminderLogs
            WHERE  Status = 'batchStart'
              AND  TargetTimeTaipei = @WorkTime
              AND  CAST(TickedAtTaipei AS DATE) = @Today
            """;
        try
        {
            var cmd = new CommandDefinition(sql, new { WorkTime = workTime, Today = today.Date }, cancellationToken: ct);
            return await conn.ExecuteScalarAsync<int?>(cmd) is not null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AttendanceReminder 冪等檢查失敗，本次照常推播：Target={Target}", workTime);
            return false;
        }
    }

    private async Task<AttendanceReminderRunResult> PushAsync(
        string type, DateTime today, string workTime, string siteUrl,
        string triggerSource, Guid? triggeredByUserId, CancellationToken ct)
    {
        // 將工作時間（"09:00"）與今日日期合成精確 targetTime，
        // 供 SQL 用「請假是否覆蓋此時刻」判斷（修正小時制請假被誤排除問題）。
        var targetTime = today.Date;
        if (TryParseHHmm(workTime, out var ts))
            targetTime = today.Date.Add(ts);

        var batchId       = Guid.NewGuid();
        var tickedAtUtc   = DateTime.UtcNow;
        var tickedAtTaipei = Clock.Now;
        var targetTimeStr = workTime;  // "HH:mm"

        // batchStart 一定要「先寫、再查收件人」，順序有兩個理由：
        //   (1) 它是 RunAsync 冪等閘的依據 —— 必須在推播前落地，才擋得住同 occurrence 的第二個實例
        //   (2) 收件人查詢若丟例外，紀錄上仍看得到「這一槽有觸發過」；
        //       舊版把它寫在查詢之後，導致「SQL 炸掉」與「排程根本沒跑」在紀錄上完全無法分辨。
        // 人數待查詢完成後再補回 UserNameSnapshot。
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
            UserNameSnapshot: null,
            Status: "batchStart",
            ErrorCategory: null,
            ErrorMessage: null,
            HttpStatusCode: null,
            DurationMs: null), ct);

        IReadOnlyList<Models.Dtos.AttendanceReminderRecipientDto> recipients;
        try
        {
            recipients = await reader.GetRecipientsAsync(targetTime, type, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "AttendanceReminder 收件人查詢失敗：type={Type} target={Target} batchId={BatchId}",
                type, targetTime.ToString("yyyy-MM-dd HH:mm"), batchId);

            await SafeWriteLogAsync(new AttendanceReminderLogRow(
                BatchId: batchId,
                TickedAt: tickedAtUtc,
                TickedAtTaipei: tickedAtTaipei,
                TargetTimeTaipei: targetTimeStr,
                ReminderType: type,
                TriggerSource: triggerSource,
                TriggeredByUserId: triggeredByUserId,
                UserId: null,
                LineUserIdSnapshot: null,
                UserNameSnapshot: null,
                Status: "failure",
                ErrorCategory: "system_error",
                ErrorMessage: Truncate($"收件人查詢失敗：{ex.Message}", 500),
                HttpStatusCode: null,
                DurationMs: null), ct);

            return new AttendanceReminderRunResult(0, 0, 0, batchId);
        }

        logger.LogInformation(
            "AttendanceReminder: type={Type} target={Target} recipientCount={Count} batchId={BatchId}",
            type, targetTime.ToString("yyyy-MM-dd HH:mm"), recipients.Count, batchId);

        await SafeUpdateRecipientCountAsync(batchId, recipients.Count, ct);

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

    /// <summary>
    /// 收件人查詢完成後，把人數補回 batchStart 那一列（供「排程有跑但 0 對象」的判讀）。
    /// 與 <see cref="SafeWriteLogAsync"/> 同樣只記 log 不 throw。
    /// </summary>
    private async Task SafeUpdateRecipientCountAsync(Guid batchId, int recipientCount, CancellationToken ct)
    {
        const string sql = """
            UPDATE AttendanceReminderLogs
            SET    UserNameSnapshot = @Note
            WHERE  BatchId = @BatchId AND Status = 'batchStart';
            """;
        try
        {
            var cmd = new CommandDefinition(
                sql, new { BatchId = batchId, Note = $"recipientCount={recipientCount}" }, cancellationToken: ct);
            await conn.ExecuteAsync(cmd);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AttendanceReminderLog 人數回寫失敗：BatchId={BatchId}", batchId);
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
