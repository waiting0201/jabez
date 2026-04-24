using Jabez.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Functions;

/// <summary>
/// 上下班前 N 分鐘 LINE 打卡提醒的 Timer Trigger。
/// Cron 每分鐘以 UTC 觸發，Service 內部以 <see cref="Common.Clock.Now"/>（台北時區）比對命中時點。
/// 選擇 UTC cron 而非 WEBSITE_TIME_ZONE / TZ 是為了相容 Linux Consumption Plan。
/// </summary>
public sealed class AttendanceReminderFunction(
    IAttendanceReminderService service,
    ILogger<AttendanceReminderFunction> logger)
{
    [Function("AttendanceReminder")]
    public async Task Run(
        [TimerTrigger("0 */1 * * * *", RunOnStartup = false)] TimerInfo timer,
        CancellationToken ct)
    {
        // Host 延遲積壓時不補跑，避免半夜重啟瞬間補跑到早上的槽位
        if (timer.IsPastDue)
            return;

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
