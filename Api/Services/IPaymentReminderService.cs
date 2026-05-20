namespace Jabez.Api.Services;

public interface IPaymentReminderService
{
    /// <summary>
    /// 執行撥款提醒。撈出未撥且 ExpectedDate 在 N 天內的 installments，
    /// 對財務部所有人各推一則彙整通知，並寫入 PaymentReminderLog。
    /// </summary>
    /// <param name="triggerSource">auto（cron）/ manual（管理員手動觸發）</param>
    /// <param name="triggeredByUserId">手動觸發者，auto 為 null</param>
    Task<PaymentReminderRunResult> RunAsync(string triggerSource, Guid? triggeredByUserId = null);
}

public sealed record PaymentReminderRunResult(
    Guid     BatchId,
    int      UpcomingItemCount,    // 撈到幾筆待提醒 installments
    int      FinanceUserCount,     // 推給多少財務人員
    int      SuccessCount,
    int      SkippedAlreadySent,
    int      FailureCount);
