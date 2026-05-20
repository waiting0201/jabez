using Jabez.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Functions;

/// <summary>
/// 撥款日將屆提醒 TimerTrigger。
/// Cron 由 app setting <c>PaymentReminderCron</c> 控制，預設每日 09:00 Taipei（= 01:00 UTC）。
/// Service 內部以 SystemSetting.PaymentReminderDaysBefore 控制提前天數（預設 3 天）。
/// 同日去重於 Service 內透過 PaymentReminderLog 處理。
/// </summary>
public sealed class PaymentReminderFunction(
    IPaymentReminderService service,
    ILogger<PaymentReminderFunction> logger)
{
    [Function("PaymentReminder")]
    public async Task Run(
        [TimerTrigger("%PaymentReminderCron%", RunOnStartup = false)] TimerInfo timer,
        CancellationToken ct)
    {
        if (timer.IsPastDue)
        {
            logger.LogWarning(
                "PaymentReminder tick 被跳過（IsPastDue=true）；下次排程：{Next}",
                timer.ScheduleStatus?.Next);
            return;
        }

        try
        {
            var result = await service.RunAsync(triggerSource: "auto");
            logger.LogInformation(
                "[PaymentReminder] auto run finished: batch={Batch}, items={Items}, finance={Fin}, success={Success}, skipped={Skip}, failure={Fail}",
                result.BatchId, result.UpcomingItemCount, result.FinanceUserCount,
                result.SuccessCount, result.SkippedAlreadySent, result.FailureCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[PaymentReminder] auto run failed");
        }
    }
}
