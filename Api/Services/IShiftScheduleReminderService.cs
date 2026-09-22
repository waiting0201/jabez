namespace Jabez.Api.Services;

/// <summary>排班提醒（四週彈性工時 §3.5 的四個 LINE 推播時點）。</summary>
public interface IShiftScheduleReminderService
{
    /// <summary>
    /// 依「今天是幾號、現在是哪個時點」決定要不要推、推哪一種。
    /// 一天最多命中一種，且同一種一天只推一次（同日去重）。
    /// </summary>
    /// <param name="forceKind">
    /// 指定要推的種類，略過「今天是幾號、現在幾點」的判斷與同日去重。
    /// 僅供 Superadmin 手動觸發（除錯 / 補發）使用，排程一律傳 null。
    /// </param>
    /// <param name="dryRun">
    /// **只解析收件人、不發送任何訊息、不寫推播紀錄**。
    /// 手動觸發一律預設為 true —— 這支端點的作用就是對外發 LINE，
    /// 誤觸的代價是真實同仁收到看不懂的通知且**無法收回**，故把安全的那一邊設為預設。
    /// </param>
    Task<ShiftScheduleReminderRunResult> RunAsync(
        string triggerSource, Guid? triggeredByUserId = null, string? forceKind = null,
        bool dryRun = false, CancellationToken ct = default);

    /// <summary>可手動觸發的四種提醒。</summary>
    static readonly string[] ValidKinds = ["schOpen", "schPending", "schDeadline", "schAuto"];
}

/// <param name="Kind">本次命中的提醒種類；null ＝ 今天此刻沒有任何提醒</param>
/// <param name="DryRun">true ＝ 本次沒有真的送出任何訊息</param>
/// <param name="Recipients">收件人姓名（乾跑時用來核對名單；實際推播時為空以免回應過大）</param>
public sealed record ShiftScheduleReminderRunResult(
    string? Kind, int Pushed, int Failed, int Skipped,
    bool DryRun = false, IReadOnlyList<string>? Recipients = null);
