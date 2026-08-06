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
        // ⚠️ IsPastDue 刻意「不」提前 return。
        // 這是每分鐘的 cron，而正式站是 Flex Consumption（scale-to-zero）—— 冷啟動幾乎必然讓
        // tick 延遲而被判 past due，舊版直接 return 等於主動放棄該槽位，錯過的那一分鐘
        // 又因為當時是 HH:mm 精確等值比對而整天不再命中（2026-07-06 / 08-06 的上班提醒即為此）。
        // 現在改由 Service 端負責：時間窗容忍延遲、batchStart 冪等閘擋重複，補跑是安全的。
        if (timer.IsPastDue)
        {
            logger.LogWarning(
                "AttendanceReminder tick 延遲（IsPastDue=true），照常執行並由冪等閘去重；下次排程：{Next}",
                timer.ScheduleStatus?.Next);
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
