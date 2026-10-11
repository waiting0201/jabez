using Jabez.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Functions;

/// <summary>
/// 排班提醒 TimerTrigger（四週彈性工時 §3.5）。
///
/// cron 由 app setting <c>ShiftScheduleReminderCron</c> 控制，現值 <c>0 0 1,4 * * *</c>
/// ＝ **每天** UTC 01:00 與 04:00（台北 09:00 / 12:00）。
///
/// ⚠ 刻意用「每天跑、由 Service 判斷今天是不是 10／20／25／26 號」而不是把日期寫進 cron：
/// 四個時點裡 25 號是 12:00、其餘三個是 09:00，單一 cron 表達不出這種組合，
/// 硬要寫就得拆成兩條 cron、兩支 Function，反而更難維護。
///
/// 與其他兩支 TimerTrigger 相同的兩個慣例：
/// <list type="bullet">
///   <item><b><c>IsPastDue</c> 不提前 return</b>：冷啟動延遲被判 past due 就整天不發，
///         而重複執行是安全的（Service 內建同日去重）。</item>
///   <item><b>吞例外</b>：避免 Functions 重試造成重複推播。</item>
/// </list>
/// </summary>
public sealed class ShiftScheduleReminderFunction(
    IShiftScheduleReminderService service,
    ILogger<ShiftScheduleReminderFunction> logger)
{
    [Function("ShiftScheduleReminder")]
    public async Task Run(
        [TimerTrigger("%ShiftScheduleReminderCron%", RunOnStartup = false)] TimerInfo timer,
        CancellationToken ct)
    {
        if (timer.IsPastDue)
            logger.LogWarning(
                "[ShiftScheduleReminder] 延遲（IsPastDue=true），照常執行並由同日去重擋重複；下次排程：{Next}",
                timer.ScheduleStatus?.Next);

        try
        {
            var result = await service.RunAsync(triggerSource: "auto", ct: ct);
            logger.LogInformation(
                "[ShiftScheduleReminder] auto run finished: kind={Kind}, pushed={Pushed}, failed={Failed}, skipped={Skipped}",
                result.Kind ?? "(none)", result.Pushed, result.Failed, result.Skipped);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[ShiftScheduleReminder] auto run failed");
        }
    }
}
