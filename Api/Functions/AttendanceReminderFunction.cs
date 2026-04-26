using Jabez.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Functions;

/// <summary>
/// 上下班前 N 分鐘 LINE 打卡提醒的 Timer Trigger。
/// Cron 以 UTC 觸發，Service 內部以 <see cref="Common.Clock.Now"/>（台北時區）比對命中時點與週末。
/// 選擇 UTC cron 而非 WEBSITE_TIME_ZONE / TZ 是為了相容 Linux Consumption Plan。
///
/// Cron 由 app setting <c>AttendanceReminderCron</c> 控制，預設只跑：
///   - 7-9 Taipei（= 23, 0, 1 UTC）
///   - 16-18 Taipei（= 8, 9, 10 UTC）
/// 其他時段不觸發以節省 Function 執行次數。
/// 週末由 Service 端過濾（cron 跨午夜時 day-of-week 無法在單一表達式中正確涵蓋週一至週五）。
/// </summary>
public sealed class AttendanceReminderFunction(
    IAttendanceReminderService service,
    ILogger<AttendanceReminderFunction> logger)
{
    [Function("AttendanceReminder")]
    public async Task Run(
        [TimerTrigger("%AttendanceReminderCron%", RunOnStartup = false)] TimerInfo timer,
        CancellationToken ct)
    {
        // Host 延遲積壓時不補跑，避免半夜重啟瞬間補跑到早上的槽位。
        // 但要留下 LogWarning 讓運維知道有一次 tick 被跳過（08:58 / 17:58 等真正的提醒時點）；
        // 若是非提醒時段（cron 仍會觸發每分鐘）的跳過，下游 RunAsync 也只是 no-op，影響可接受。
        if (timer.IsPastDue)
        {
            logger.LogWarning(
                "AttendanceReminder tick 被跳過（IsPastDue=true），可能是 host 延遲或重啟造成；下次排程：{Next}",
                timer.ScheduleStatus?.Next);
            return;
        }

        try
        {
            await service.RunAsync(ct);
        }
        catch (Exception ex)
        {
            // 吞例外只記 log，避免 Functions 視為失敗並重試（重試會造成重複推播）
            logger.LogError(ex, "AttendanceReminder 執行失敗");
        }
    }
}
