namespace Jabez.Api.Services;

public interface IAttendanceReminderService
{
    /// <summary>排程自動呼叫（判斷當前時點 + 推播）。</summary>
    Task RunAsync(CancellationToken ct = default);

    /// <summary>
    /// 手動觸發（除錯用）：略過時點與週末檢查，但仍套用「已打卡 / 請假中 / LineUserId 為空 / 已離職」等過濾條件。
    /// </summary>
    /// <param name="type">"clockIn" 或 "clockOut"</param>
    /// <returns>實際推播人數</returns>
    Task<int> ForceRunAsync(string type, CancellationToken ct = default);
}
