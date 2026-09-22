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
    IWorkdayScheduleProvider workdaySchedule,
    IShiftScheduleReadService shiftReader,
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
    ///
    /// 2026-09-09 由 10 → 30：2026-09-08 的下班提醒整批沒發（該日無 18:00 的 batchStart，
    /// 全體員工都沒收到），而同一時段有 9 人正常打下班卡 —— App 是活的，是 17:58–18:07
    /// 這 10 分鐘內一次 tick 都沒觸發到。延後發送不會誤擾任何人：收件人 SQL 本來就排除
    /// 「今日已打該類型卡」的人，晚發只會發給還沒打卡的人，語意仍然成立。
    /// 窗尾（09:28 / 18:28 Taipei）仍落在 cron 涵蓋時段（台北 7-9 / 16-18 時）內，故不必改 cron。
    /// </summary>
    private const int WindowMinutes = 30;

    /// <summary>推播間隔（毫秒）：避免一次性 burst 觸發 LINE 速率限制。</summary>
    private const int InterPushDelayMs = 100;

    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = Clock.Now;

        // 週末只提醒排班制員工（賣店 / 營業所照常營業）；一人都沒有就維持整批不推。
        // 平日刻意不看行事曆（國定假日照推），沿用既有語意。
        var isWeekend = now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        if (isWeekend && !await db.Users.AsNoTracking()
                .AnyAsync(u => u.IsShiftWorker && u.Status == "active", ct))
            return;

        var setting = await db.SystemSettings.AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(ct);
        if (setting is null)
            return;

        // 四週彈性工時切換後改走個人化提醒；切換前一律沿用下方原有流程，行為完全不變。
        var switchDate = await workdaySchedule.GetSwitchDateAsync();
        if (switchDate is { } sd && now.Date >= sd.Date)
        {
            await RunFlexibleAsync(now, setting, ct);
            return;
        }

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

        await PushAsync(type, now.Date, workTime, setting.SiteUrl, "auto", null, isWeekend, ct);
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
        // 手動觸發沿用同一條「今天誰要上班」規則：六日只推排班制員工
        var forcedWeekend = Clock.Today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        return await PushAsync(type, Clock.Today, workTime, setting.SiteUrl, "manual", triggeredByUserId, forcedWeekend, ct);
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
    /// <param name="slot">
    /// 槽別（寫在 batchStart 列的 ReminderType）。舊制的兩槽一律是 "batchStart"；
    /// 新制的兩個 12:55 提醒各佔一槽，靠這個參數區分 ——
    /// TargetTimeTaipei 只有 5 字元（HH:mm），塞不下槽名。
    /// </param>
    private async Task<bool> HasBatchStartedTodayAsync(
        DateTime today, string workTime, CancellationToken ct, string slot = "batchStart")
    {
        const string sql = """
            SELECT TOP 1 1
            FROM   AttendanceReminderLogs
            WHERE  Status = 'batchStart'
              AND  TargetTimeTaipei = @WorkTime
              AND  ReminderType = @Slot
              AND  CAST(TickedAtTaipei AS DATE) = @Today
            """;
        try
        {
            var cmd = new CommandDefinition(sql, new { WorkTime = workTime, Today = today.Date, Slot = slot }, cancellationToken: ct);
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
        string triggerSource, Guid? triggeredByUserId, bool shiftWorkersOnly, CancellationToken ct)
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
            recipients = await reader.GetRecipientsAsync(targetTime, type, shiftWorkersOnly, ct);
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
                // 剩餘分鐘數以「推播當下」實際算，不用 LeadMinutes 常數：命中窗有 WindowMinutes 分鐘，
                // tick 延遲時目標時刻可能已過，寫死常數會推出「09:25 說再 2 分鐘上班」。
                // 準時的 tick（目標 −LeadMinutes）算出來仍是 LeadMinutes，文案與過去一致。
                var minutesUntil = (int)Math.Round((targetTime - Clock.Now).TotalMinutes, MidpointRounding.AwayFromZero);
                var flex = LineFlexMessageBuilder.BuildAttendanceReminderMessage(
                    type, r.UserName, minutesUntil, workTime, linkUrl);
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

    /// <summary>寫一筆 batchStart 足跡（不含收件人）。</summary>
    private async Task WriteBatchStartAsync(
        Guid batchId, DateTime now, string slotTime, string slot,
        string triggerSource, Guid? triggeredByUserId, CancellationToken ct)
    {
        await SafeWriteLogAsync(new AttendanceReminderLogRow(
            BatchId: batchId,
            TickedAt: DateTime.UtcNow,
            TickedAtTaipei: now,
            TargetTimeTaipei: slotTime,
            ReminderType: slot,
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
    }

    /// <summary>推播給單一收件人並寫 success / failure 紀錄。</summary>
    private async Task PushOneAsync(
        Guid batchId, DateTime now, string slotTime, string reminderType,
        string triggerSource, Guid? triggeredByUserId,
        Models.Dtos.AttendanceReminderRecipientDto r, object message, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        PushResult pr;
        try
        {
            pr = await lineService.PushMessageAsync(r.LineUserId, message);
        }
        catch (Exception ex)
        {
            pr = new PushResult(false, null, "system_error", Truncate(ex.Message, 500));
            logger.LogError(ex, "打卡提醒推播例外（系統錯誤）：UserId={UserId}, Name={Name}", r.UserId, r.UserName);
        }
        finally
        {
            sw.Stop();
        }

        await SafeWriteLogAsync(new AttendanceReminderLogRow(
            BatchId: batchId,
            TickedAt: DateTime.UtcNow,
            TickedAtTaipei: now,
            TargetTimeTaipei: slotTime,
            ReminderType: reminderType,
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

        await Task.Delay(InterPushDelayMs, ct);
    }

    // ── 四週彈性工時：個人化下班提醒與當日狀態文案 ──────────────────────────
    //
    // ⚠ **這裡是整個提醒模組唯一需要改架構的地方**。
    // 舊制的冪等閘 key 是「台北日期 ＋ TargetTimeTaipei（全公司同一個 HH:mm）」且在**整批層級**：
    // batchStart 先寫、再查收件人。新制的下班時點是「實際上班打卡 ＋ 9 小時 − 2 分」，每人不同 ——
    // 沿用整批閘的話，**第一個人推播寫下的 batchStart 會把其餘時點的人整批擋死**，
    // 那些人整天收不到提醒，而且紀錄上看起來一切正常。
    // 故下班提醒改為「每人每日每類型一次」的去重（GetAlreadyPushedUserIdsAsync）。
    //
    // 上班提醒（08:58）與兩個 12:55 半天假提醒仍是**固定時點**，繼續沿用整批閘，各佔一個槽。

    /// <summary>上午半天假者的交接提醒時刻（取代當日 08:58 的上班提醒 —— 那時人還在休假）。</summary>
    private static readonly TimeOnly HalfDayHandoverAt = new(12, 55);

    private const string SlotHalfDayAm = "amHandover";
    private const string SlotHalfDayPm = "pmHandover";

    /// <summary>
    /// 新制的每分鐘處理：三種提醒各自獨立判斷，彼此不互相擋。
    /// </summary>
    private async Task RunFlexibleAsync(DateTime now, Models.Entities.SystemSetting setting, CancellationToken ct)
    {
        // ① 上班提醒（固定 08:58）：依當日日別發三種文案
        if (IsWithinWindow(now, setting.WorkStartTime)
            && !await HasBatchStartedTodayAsync(now.Date, setting.WorkStartTime, ct))
        {
            await PushClockInByDayTypeAsync(now, setting, ct);
        }

        // ② 半天假 12:55 交接提醒（兩個獨立的固定時點槽）
        var handover = HalfDayHandoverAt.ToString("HH\\:mm");
        if (IsWithinWindow(now, handover))
        {
            await PushHalfDayHandoverAsync(now, setting, "am", SlotHalfDayAm, handover, ct);
            await PushHalfDayHandoverAsync(now, setting, "pm", SlotHalfDayPm, handover, ct);
        }

        // ③ 個人化下班提醒：每個 tick 都要看，時點每人不同
        await PushPersonalClockOutAsync(now, setting, ct);
    }

    /// <summary>
    /// 上班提醒：依個人當日日別發三種文案（上班日 / 休假日 / 例假日）。
    /// 國定假日者不推（免出勤，且未被排活動日時本來就不該來）。
    /// </summary>
    private async Task PushClockInByDayTypeAsync(DateTime now, Models.Entities.SystemSetting setting, CancellationToken ct)
    {
        var targetTime = now.Date.Add(ParseOrDefault(setting.WorkStartTime, new TimeOnly(9, 0)).ToTimeSpan());

        // 收件人沿用既有 SQL（已排除今日已打卡、請假涵蓋該時刻者）；
        // 休假日 / 例假日的人本來就不會打卡，故會留在名單內，再依日別分流文案。
        var recipients = await reader.GetRecipientsAsync(targetTime, "clockIn", shiftWorkersOnly: false, ct);
        if (recipients.Count == 0) return;

        var dayTypes = await shiftReader.ResolveRangeForUsersAsync(
            [.. recipients.Select(r => r.UserId)], now.Date, now.Date);

        var batchId = Guid.NewGuid();
        await WriteBatchStartAsync(batchId, now, setting.WorkStartTime, "batchStart", "auto", null, ct);

        int minutesUntil = (int)Math.Round((targetTime - now).TotalMinutes);

        foreach (var r in recipients)
        {
            var dayType = dayTypes.TryGetValue((r.UserId, now.Date), out var t) ? t : WorkDayTypes.Work;
            if (dayType == WorkDayTypes.PublicHoliday) continue;   // 國定假日免出勤，不打擾

            var message = dayType switch
            {
                WorkDayTypes.StatutoryOff => LineFlexMessageBuilder.BuildShiftDayNoticeMessage(
                    r.UserName, "例假日",
                    "本日為您排定的例假日，依法嚴禁出勤。如遇業主或承辦人提出公務需求，"
                    + "請禮貌告知將於上班日再行處理，或轉由職務代理人協助處理。", setting.SiteUrl),

                WorkDayTypes.RestDay => LineFlexMessageBuilder.BuildShiftDayNoticeMessage(
                    r.UserName, "休假日",
                    "本日為您排定的休假日。如因緊急公務必要需求需於休假日加班者，"
                    + "請務必事前至《加班申請系統》提出加班申請，經主管核准後方可出勤。", setting.SiteUrl),

                _ => LineFlexMessageBuilder.BuildAttendanceReminderMessage(
                    "clockIn", r.UserName, minutesUntil, setting.WorkStartTime, setting.SiteUrl),
            };

            await PushOneAsync(batchId, now, setting.WorkStartTime, "clockIn", "auto", null, r, message, ct);
        }
    }

    /// <summary>半天假 12:55 交接提醒。適用**所有半天假假別**，不限補休。</summary>
    private async Task PushHalfDayHandoverAsync(
        DateTime now, Models.Entities.SystemSetting setting, string segment, string slot, string slotTime, CancellationToken ct)
    {
        if (await HasBatchStartedTodayAsync(now.Date, slotTime, ct, slot)) return;

        var boundary = now.Date.AddHours(13);
        var recipients = await reader.GetHalfDayLeaveRecipientsAsync(now.Date, segment, boundary, ct);

        var batchId = Guid.NewGuid();
        await WriteBatchStartAsync(batchId, now, slotTime, slot, "auto", null, ct);
        if (recipients.Count == 0) return;

        var (title, body) = segment == "am"
            ? ("上午假將屆", "上午假時數將屆，請準備完成上班打卡！")
            : ("準備休假", "請準備開始休假，並記得先完成下班打卡！");

        foreach (var r in recipients)
        {
            var message = LineFlexMessageBuilder.BuildShiftDayNoticeMessage(r.UserName, title, body, setting.SiteUrl);
            await PushOneAsync(batchId, now, slotTime, slot, "auto", null, r, message, ct);
        }
    }

    /// <summary>
    /// 個人化下班提醒：時點 ＝ 實際上班打卡 ＋ 9 小時 − 2 分（請上午半天假者 ＋4 小時）。
    /// **去重下沉到每人每日一次**，不走整批閘。
    /// </summary>
    private async Task PushPersonalClockOutAsync(DateTime now, Models.Entities.SystemSetting setting, CancellationToken ct)
    {
        var candidates = await reader.GetClockOutCandidatesAsync(now.Date, ct);
        if (candidates.Count == 0) return;

        var schedule = WorkdayHours.For(now.Date, await workdaySchedule.GetSwitchDateAsync());
        var pushed   = (await reader.GetAlreadyPushedUserIdsAsync(now.Date, "clockOut", ct)).ToHashSet();

        var due = candidates
            .Where(c => !pushed.Contains(c.UserId))
            .Select(c => (c, At: ClockRules.ExpectedClockOut(c.ClockInTime, schedule, c.AfternoonOnly)
                                             .AddMinutes(-LeadMinutes)))
            .Where(x => now >= x.At && now < x.At.AddMinutes(WindowMinutes))
            .ToList();

        if (due.Count == 0) return;

        var batchId = Guid.NewGuid();
        // batchStart 只作為「這一分鐘有處理個人化下班提醒」的足跡，**不是冪等閘**
        await WriteBatchStartAsync(batchId, now, now.ToString("HH\\:mm"), "clockOut", "auto", null, ct);

        foreach (var (c, at) in due)
        {
            int minutesUntil = (int)Math.Round((at.AddMinutes(LeadMinutes) - now).TotalMinutes);
            var message = LineFlexMessageBuilder.BuildAttendanceReminderMessage(
                "clockOut", c.UserName, minutesUntil,
                at.AddMinutes(LeadMinutes).ToString("HH\\:mm"), setting.SiteUrl);

            await PushOneAsync(batchId, now, now.ToString("HH\\:mm"), "clockOut", "auto", null,
                new Models.Dtos.AttendanceReminderRecipientDto(c.UserId, c.LineUserId, c.UserName), message, ct);
        }
    }

    private static TimeOnly ParseOrDefault(string hhmm, TimeOnly fallback) =>
        TryParseHHmm(hhmm, out var ts) ? TimeOnly.FromTimeSpan(ts) : fallback;
}
