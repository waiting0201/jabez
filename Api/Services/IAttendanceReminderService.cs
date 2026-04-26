namespace Jabez.Api.Services;

/// <summary>打卡提醒執行結果。</summary>
/// <param name="RecipientCount">符合過濾條件的對象人數（嘗試推播的數量）</param>
/// <param name="PushedCount">LINE API 回 2xx 的成功數</param>
/// <param name="FailureCount">推播失敗數（含未加好友、token 過期、網路錯誤等）</param>
public sealed record AttendanceReminderRunResult(int RecipientCount, int PushedCount, int FailureCount);

public interface IAttendanceReminderService
{
    /// <summary>排程自動呼叫（判斷當前時點 + 推播）。</summary>
    Task RunAsync(CancellationToken ct = default);

    /// <summary>
    /// 手動觸發（除錯用）：略過時點與週末檢查，但仍套用「已打卡 / 請假覆蓋目標時刻 / LineUserId 為空 / 已離職」等過濾條件。
    /// </summary>
    /// <param name="type">"clockIn" 或 "clockOut"</param>
    /// <returns>對象人數、實際成功數、失敗數</returns>
    Task<AttendanceReminderRunResult> ForceRunAsync(string type, CancellationToken ct = default);
}
