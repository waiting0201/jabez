using Jabez.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Functions;

/// <summary>
/// 天然災害停班（颱風假）公告同步 TimerTrigger。
/// Cron 由 app setting <c>DisasterDayOffCron</c> 控制，建議每 10 分鐘（<c>0 */10 * * * *</c>）。
/// 去重：Service 以公告頁「更新時間」比對 DisasterDayOffFetchLogs，未變就略過（不寫紀錄、不通知），故高頻輪詢是安全的。
/// 比照 <see cref="PaymentReminderFunction"/>：IsPastDue 不跳過、例外只記 log（下個 tick 自然重試）。
/// </summary>
public sealed class DisasterDayOffFunction(
    IDisasterDayOffService service,
    ILogger<DisasterDayOffFunction> logger)
{
    [Function("DisasterDayOff")]
    public async Task Run(
        [TimerTrigger("%DisasterDayOffCron%", RunOnStartup = false)] TimerInfo timer,
        CancellationToken ct)
    {
        if (timer.IsPastDue)
            logger.LogWarning("DisasterDayOff tick 延遲（IsPastDue=true），照常執行；去重由更新時間比對處理");

        try
        {
            var log = await service.SyncAsync("auto", force: false, ct);
            if (log is not null)
                logger.LogInformation("[DisasterDayOff] {Summary}（success={Success}，來源更新時間 {Updated}）",
                    log.Summary, log.Success, log.SourceUpdatedAt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[DisasterDayOff] auto run failed");
        }
    }
}
