using System.Collections.Concurrent;
using Jabez.Api.Common;

namespace Jabez.Api.Services;

/// <summary>
/// 每人（JWT sub）速率限制（static、記憶體滑動視窗，不引入新套件，2026-10 防濫用）。
/// 保護會燒付費額度 / 外部 API 的端點（Gemini OCR、GCIS 統編查詢）：超過上限回 429。
///
/// <b>已知限制</b>：計數存在單一 Function 實例的記憶體內；Azure Functions 橫向擴充成多實例時，
/// 每個實例各自計數（實際上限 ≈ 上限 × 實例數），冷啟動 / 重啟後歸零。
/// 本系統為內部小規模使用，此精度足以擋住迴圈濫用；若需精確全域上限須改走 DB 計數（比照 LoginAttemptTracker）。
///
/// 數值集中在 <see cref="Limits"/>，要調整只改這裡。
/// </summary>
public static class UserRateLimiter
{
    /// <summary>一組限制：視窗內最多 Max 次。</summary>
    public sealed record Rule(string Name, TimeSpan Window, int Max);

    public static class Limits
    {
        // OCR（發票 / 報價單共用同一個額度桶，避免兩支端點各打滿一次）
        public static readonly Rule OcrPerMinute = new("ocr-min", TimeSpan.FromMinutes(1), 10);
        public static readonly Rule OcrPerDay    = new("ocr-day", TimeSpan.FromDays(1), 200);
        // GCIS 統編查詢
        public static readonly Rule GcisPerMinute = new("gcis-min", TimeSpan.FromMinutes(1), 20);
    }

    private static readonly ConcurrentDictionary<(string Key, string Rule), Queue<DateTime>> Hits = new();
    private static long _calls;

    /// <summary>
    /// 記一次呼叫並檢查所有規則；任一規則超限就丟 429（此次不計入）。
    /// 先檢查全部、都通過才一起記錄，避免被擋的呼叫仍消耗其他桶的額度。
    /// </summary>
    public static void Enforce(string userKey, params Rule[] rules)
    {
        var now = DateTime.UtcNow;

        if (Interlocked.Increment(ref _calls) % 500 == 0) Sweep(now);

        var queues = new List<(Rule Rule, Queue<DateTime> Q)>(rules.Length);
        foreach (var r in rules)
            queues.Add((r, Hits.GetOrAdd((userKey, r.Name), _ => new Queue<DateTime>())));

        // 以固定順序（規則名稱）逐一鎖定，避免多規則同時鎖造成死結
        var ordered = queues.OrderBy(x => x.Rule.Name, StringComparer.Ordinal).ToList();
        LockAll(ordered, 0, () =>
        {
            foreach (var (rule, q) in ordered)
            {
                while (q.Count > 0 && now - q.Peek() >= rule.Window) q.Dequeue();
                if (q.Count >= rule.Max)
                    throw new AppException(
                        $"操作過於頻繁，請稍後再試（{DescribeWindow(rule.Window)}內最多 {rule.Max} 次）。", 429);
            }
            foreach (var (_, q) in ordered) q.Enqueue(now);
        });
    }

    private static void LockAll(List<(Rule Rule, Queue<DateTime> Q)> items, int i, Action body)
    {
        if (i == items.Count) { body(); return; }
        lock (items[i].Q) LockAll(items, i + 1, body);
    }

    private static string DescribeWindow(TimeSpan w) =>
        w.TotalDays >= 1 ? "每日" : w.TotalMinutes >= 1 ? $"每 {(int)w.TotalMinutes} 分鐘" : $"每 {(int)w.TotalSeconds} 秒";

    /// <summary>清掉久未使用的鍵，避免記憶體無限成長。</summary>
    private static void Sweep(DateTime now)
    {
        foreach (var (k, q) in Hits)
        {
            lock (q)
            {
                // 空佇列，或最舊一筆已超過最長視窗（1 日）＝整組都過期，可丟
                if (q.Count == 0 || now - q.Peek() >= TimeSpan.FromDays(1))
                    Hits.TryRemove(k, out _);
            }
        }
    }
}
