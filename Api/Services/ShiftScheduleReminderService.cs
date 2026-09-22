using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Services;

/// <summary>
/// 排班提醒（四週彈性工時 §3.5）的四個時點：
///
/// | 時點 | 對象 | 內容 |
/// |---|---|---|
/// | 10 號 09:00 | 全員 | 次月排班已開放填寫 |
/// | 20 號 09:00 | **次月尚未完成排班者** | 排班未完成提醒 |
/// | 25 號 12:00 | 次月尚未完成排班者 | 今日 23:59:59 截止 |
/// | 26 號 09:00 | **前一日被自動排班者** | 系統已為您自動排班 |
///
/// 同日去重：同一種提醒一天只推一次（沿用 <c>AttendanceReminderLogs</c> 的 batchStart 慣例，
/// 以 <c>ReminderType</c> 區分槽別）。冷啟動延遲導致同一時點被跑兩次時靠它擋掉。
///
/// ⚠ **制度未切換時不推**（<c>SystemSetting.FlexibleWorkStartDate</c> 為 null）——
/// 同仁根本還沒有排班功能可用，推了只會造成困惑。
/// </summary>
public sealed class ShiftScheduleReminderService(
    AppDbContext db,
    IAttendanceReminderReadService reminderReader,
    IWorkdayScheduleProvider workdaySchedule,
    ILineService lineService,
    ILogger<ShiftScheduleReminderService> logger) : IShiftScheduleReminderService
{
    /// <summary>推播間隔（毫秒）：避免 burst 觸發 LINE 速率限制。</summary>
    private const int InterPushDelayMs = 100;

    public async Task<ShiftScheduleReminderRunResult> RunAsync(
        string triggerSource, Guid? triggeredByUserId = null, string? forceKind = null,
        bool dryRun = false, CancellationToken ct = default)
    {
        var now = Clock.Now;

        // 制度尚未切換 → 不推。同仁還沒有排班功能可用。
        if (await workdaySchedule.GetSwitchDateAsync() is not { } switchDate || now.Date < switchDate.Date)
            return new ShiftScheduleReminderRunResult(null, 0, 0, 0);

        var kind = forceKind ?? ResolveKind(now);
        if (kind is null)
            return new ShiftScheduleReminderRunResult(null, 0, 0, 0);

        var setting = await db.SystemSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync(ct);
        if (setting is null)
            return new ShiftScheduleReminderRunResult(kind, 0, 0, 0);

        // 同日去重
        if (triggerSource == "auto" && await HasPushedTodayAsync(now.Date, kind, ct))
            return new ShiftScheduleReminderRunResult(kind, 0, 0, 1);

        // 目標月份：10 / 20 / 25 號排的是**次月**；26 號通知的是前一日自動排好的那個月（也是次月）
        var target = new DateTime(now.Year, now.Month, 1).AddMonths(1);

        var recipients = await ResolveRecipientsAsync(kind, target, ct);

        // 乾跑：只回名單，不發送、不寫紀錄
        if (dryRun)
            return new ShiftScheduleReminderRunResult(
                kind, 0, 0, 0, DryRun: true, Recipients: [.. recipients.Select(r => r.Name)]);

        var batchId = Guid.NewGuid();
        await WriteLogAsync(batchId, now, kind, triggerSource, triggeredByUserId, null, null, null, "batchStart", ct);

        int pushed = 0, failed = 0;
        foreach (var r in recipients)
        {
            ct.ThrowIfCancellationRequested();

            var (title, body) = BuildText(kind, target);
            var message = LineFlexMessageBuilder.BuildShiftDayNoticeMessage(r.Name, title, body, setting.SiteUrl);

            try
            {
                var pr = await lineService.PushMessageAsync(r.LineUserId, message);
                if (pr.Success) pushed++; else failed++;
                await WriteLogAsync(batchId, now, kind, triggerSource, triggeredByUserId,
                    r.Id, r.LineUserId, r.Name, pr.Success ? "success" : "failure", ct);
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogError(ex, "[ShiftScheduleReminder] 推播例外：UserId={UserId}", r.Id);
                await WriteLogAsync(batchId, now, kind, triggerSource, triggeredByUserId,
                    r.Id, r.LineUserId, r.Name, "failure", ct);
            }

            await Task.Delay(InterPushDelayMs, ct);
        }

        return new ShiftScheduleReminderRunResult(kind, pushed, failed, 0);
    }

    /// <summary>今天此刻命中哪一種提醒。時點以「小時」比對，容忍分鐘級的 cron 延遲。</summary>
    private static string? ResolveKind(DateTime now) => (now.Day, now.Hour) switch
    {
        (ShiftScheduleWindow.OpenFromDay, 9) => "schOpen",       // 10 號 09:00
        (20, 9)                              => "schPending",    // 20 號 09:00
        (ShiftScheduleWindow.OpenToDay, 12)  => "schDeadline",   // 25 號 12:00
        (26, 9)                              => "schAuto",       // 26 號 09:00
        _                                    => null,
    };

    private async Task<List<RecipientRow>> ResolveRecipientsAsync(string kind, DateTime target, CancellationToken ct)
    {
        var baseQuery = db.Users.AsNoTracking()
            .Where(u => u.Status == "active" && !u.IsSuperAdmin
                     && u.LineUserId != null && u.LineUserId != "");

        // 「已完成排班」＝ ShiftScheduleMonths 有該月且狀態為 committed
        var committed = db.ShiftScheduleMonths.AsNoTracking()
            .Where(m => m.Year == target.Year && m.Month == target.Month
                     && m.Status == ShiftScheduleMonthStatus.Committed)
            .Select(m => m.UserId);

        var query = kind switch
        {
            // 尚未完成排班者
            "schPending" or "schDeadline" => baseQuery.Where(u => !committed.Contains(u.Id)),

            // 前一日被自動排班者
            "schAuto" => baseQuery.Where(u => db.ShiftScheduleMonths
                .Any(m => m.UserId == u.Id && m.Year == target.Year && m.Month == target.Month
                       && m.Status == ShiftScheduleMonthStatus.Auto)),

            _ => baseQuery,   // schOpen：全員
        };

        return await query
            .Select(u => new RecipientRow(u.Id, u.Name, u.LineUserId!))
            .ToListAsync(ct);
    }

    private static (string Title, string Body) BuildText(string kind, DateTime target) => kind switch
    {
        "schOpen" => ($"{target:M} 月排班已開放",
            $"{target:yyyy 年 M 月}班表已開放填寫，請於本月 {ShiftScheduleWindow.OpenToDay} 日 23:59 前完成排定"
            + $"（例假日 4 天、休假日 {ShiftScheduleValidator.RequiredRestDaysFor(target.Year, target.Month)} 天）。"),

        "schPending" => ($"{target:M} 月排班尚未完成",
            $"您的 {target:yyyy 年 M 月}班表尚未完成排定，請於本月 {ShiftScheduleWindow.OpenToDay} 日 23:59 前完成，"
            + "逾期將由系統自動為您排班。"),

        "schDeadline" => ($"{target:M} 月排班今日截止",
            $"新增／修改排班今日截止，請於 23:59:59 前完成儲存。逾期將由系統自動為您排班。"),

        "schAuto" => ("系統已為您自動排班",
            $"您未於期限內完成 {target:yyyy 年 M 月}排班，系統已依規定為您排定例假與休假日。"
            + "請至〈個人排班〉確認；如需調整，請提出〈改班申請〉。"),

        _ => ("排班提醒", ""),
    };

    private async Task<bool> HasPushedTodayAsync(DateTime today, string kind, CancellationToken ct)
    {
        try
        {
            return await db.AttendanceReminderLogs.AsNoTracking()
                .AnyAsync(l => l.Status == "batchStart"
                            && l.ReminderType == kind
                            && l.TickedAtTaipei >= today.Date
                            && l.TickedAtTaipei < today.Date.AddDays(1), ct);
        }
        catch (Exception ex)
        {
            // 查詢失敗一律回 false —— 寧可重複推播，也不要因 log 表出狀況而整天不發
            logger.LogError(ex, "[ShiftScheduleReminder] 同日去重檢查失敗，本次照常推播：kind={Kind}", kind);
            return false;
        }
    }

    private async Task WriteLogAsync(
        Guid batchId, DateTime now, string kind, string triggerSource, Guid? triggeredBy,
        Guid? userId, string? lineUserId, string? userName, string status, CancellationToken ct)
    {
        try
        {
            db.AttendanceReminderLogs.Add(new AttendanceReminderLog
            {
                BatchId            = batchId,
                TickedAt           = DateTime.UtcNow,
                TickedAtTaipei     = now,
                TargetTimeTaipei   = now.ToString("HH\\:mm"),
                ReminderType       = kind,
                TriggerSource      = triggerSource,
                TriggeredByUserId  = triggeredBy,
                UserId             = userId,
                LineUserIdSnapshot = lineUserId,
                UserNameSnapshot   = userName,
                Status             = status,
                CreatedAt          = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // 寫紀錄失敗不阻斷推播主流程
            logger.LogError(ex, "[ShiftScheduleReminder] 寫入紀錄失敗：kind={Kind} userId={UserId}", kind, userId);
        }
    }

    private sealed record RecipientRow(Guid Id, string Name, string LineUserId);
}
