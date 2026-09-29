namespace Jabez.Api.Common;

/// <summary>
/// 排班檢核結果。<see cref="CanSave"/> ＝ 三條硬性檢核全過；<see cref="Warnings"/> 不影響儲存。
/// </summary>
/// <param name="CanSave">是否允許儲存（例假排滿 ∧ 關卡 A ∧ 關卡 B）</param>
/// <param name="Blocks">擋存原因（給使用者看的完整句子）</param>
/// <param name="Warnings">警示（可存，但要提示）</param>
/// <param name="StatutoryOffCount">本月已排例假天數</param>
/// <param name="RestDayCount">本月已排休假天數</param>
/// <param name="RequiredStatutoryOff">本月應排例假天數（恆 4）</param>
/// <param name="RequiredRestDay">
/// 本月應排休假天數 ＝ <paramref name="RequiredOffDays"/> − max(4, 已排例假)。
/// 例假多排幾天，休假就少排幾天（見 <see cref="ShiftScheduleValidator"/>「例假由休假轉入」）。
/// </param>
/// <param name="RequiredOffDays">本月例假＋休假合計應排天數（31 日曆月為 9，其餘 8）</param>
public sealed record ShiftScheduleValidationResult(
    bool CanSave,
    IReadOnlyList<string> Blocks,
    IReadOnlyList<string> Warnings,
    int StatutoryOffCount,
    int RestDayCount,
    int RequiredStatutoryOff,
    int RequiredRestDay,
    int RequiredOffDays);

/// <summary>
/// 個人排班「能不能存」的單一真相（純函式、無 I/O，比照 <see cref="OvertimePayCalculator"/>）。
///
/// 擋存判準（2026-09-16 客戶回覆改版，原規格為三項皆警示不擋存）：
/// <code>例假 4 天已排滿 ∧ 關卡 A 通過 ∧ 關卡 B 通過 ⇒ 可儲存</code>
/// 休假未排滿**不影響儲存**，只出警示。
///
/// 設計理由：例假日是《勞基法》強制休息日（違者罰鍰 2 萬～100 萬元），
/// 關卡 A／B 又都是「例假排得夠不夠散」的衍生條件 —— 三者本質是同一件事，故一併硬擋。
/// 休假日屬免出勤日、無罰則，允許半成品暫存。
///
/// ⚠ **空白月曆的第一次儲存必然被擋**（全月皆上班日時關卡 A／B 必不過）。
/// 這是客戶確認過的預期行為，但 UI 須於**進入畫面時**就提示「請先排定 4 天例假日」，
/// 不可等到按下儲存才報錯。
///
/// ⚠ **關卡 B 會跨出當月**：滾動 14 天視窗必須併入前一個月月底與次月月初的已定案班表，
/// 否則月初／月底永遠算不準。前月為歷史（唯讀、已定案）可直接讀；
/// 次月若尚未排定則**該側不檢核**（不可把未排的日子當成上班日，否則會誤擋）。
///
/// <b>例假由休假轉入（2026-09-29 決議）</b>：關卡 B 是滾動視窗，等同要求例假平均每 7 天至少一天，
/// 一年至少 52 天；但配額是每月 4 天、一年只有 48 天。兩者差 4 天，故每位同仁一年約有 4 個月
/// **必然**要排 5 天例假才過得了關卡 B（例：10 月最後一個例假落在 10/24，11 月就非得 5 天不可）。
/// 決議維持「每月休息總天數不變」（8 天；31 日曆月 9 天）：例假至少 4 天，
/// **多排的例假從休假扣**，即 休假應排 ＝ 休息總天數 − max(4, 已排例假)。
/// 員工與公司都沒有多放或少放一天，只是其中一天的性質由休假變成例假（對員工只有更嚴格的保護）。
/// </summary>
public static class ShiftScheduleValidator
{
    /// <summary>每月應排例假天數（法定 4 週至少 4 天）。</summary>
    public const int RequiredStatutoryOffDays = 4;

    /// <summary>連續上班日上限（勞基法：不得超過 12 日）。</summary>
    public const int MaxConsecutiveWorkDays = 12;

    /// <summary>滾動視窗天數（每 2 週內至少 2 天例假）。</summary>
    public const int RollingWindowDays = 14;

    /// <summary>滾動視窗內的最低例假天數。</summary>
    public const int MinStatutoryOffPerWindow = 2;

    /// <summary>該月應排休假天數：31 日曆月為 5 天，其餘 4 天。</summary>
    /// <remarks>
    /// 這是「例假剛好 4 天」時的**基準**休假天數，供自動排班初始挑選與提醒文字使用。
    /// 檢核與畫面上的應排天數一律用帶 <c>statutoryOff</c> 的多載。
    /// </remarks>
    public static int RequiredRestDaysFor(int year, int month) =>
        DateTime.DaysInMonth(year, month) == 31 ? 5 : 4;

    /// <summary>該月例假＋休假合計應排天數：31 日曆月為 9 天，其餘 8 天。</summary>
    public static int RequiredOffDaysFor(int year, int month) =>
        RequiredStatutoryOffDays + RequiredRestDaysFor(year, month);

    /// <summary>
    /// 已排 <paramref name="statutoryOff"/> 天例假時，該月還應排幾天休假：
    /// 休息總天數 − max(4, 已排例假)。例假未滿 4 天時仍以 4 天計（例假不足另由擋存處理）。
    /// </summary>
    public static int RequiredRestDaysFor(int year, int month, int statutoryOff) =>
        Math.Max(0, RequiredOffDaysFor(year, month) - Math.Max(RequiredStatutoryOffDays, statutoryOff));

    /// <summary>
    /// 檢核某人某月的班表。
    /// </summary>
    /// <param name="year">目標年</param>
    /// <param name="month">目標月</param>
    /// <param name="monthDays">
    /// 本月**每一天**的日別（含國定假日）。key 為日期（只取日期部分），value 為 <see cref="WorkDayTypes"/> 四值。
    /// </param>
    /// <param name="contextDays">
    /// 前月月底與次月月初的**已定案**班表，供關卡 A／B 跨月使用。
    /// 只放「確定知道」的日子 —— 次月尚未排定就不要放，該側會自動不檢核。
    /// </param>
    public static ShiftScheduleValidationResult Validate(
        int year, int month,
        IReadOnlyDictionary<DateTime, string> monthDays,
        IReadOnlyDictionary<DateTime, string>? contextDays = null)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);

        var blocks   = new List<string>();
        var warnings = new List<string>();

        // ── 配額：只數當月 ────────────────────────────────────────────
        int statutoryOff = monthDays.Count(kv => kv.Key >= monthStart && kv.Key <= monthEnd
                                              && kv.Value == WorkDayTypes.StatutoryOff);
        int restDay      = monthDays.Count(kv => kv.Key >= monthStart && kv.Key <= monthEnd
                                              && kv.Value == WorkDayTypes.RestDay);
        int requiredOff  = RequiredOffDaysFor(year, month);
        int requiredRest = RequiredRestDaysFor(year, month, statutoryOff);

        if (statutoryOff < RequiredStatutoryOffDays)
            blocks.Add($"請先排定 {RequiredStatutoryOffDays} 天例假日（目前 {statutoryOff} 天）。");
        else if (statutoryOff > RequiredStatutoryOffDays)
            warnings.Add($"例假日排了 {statutoryOff} 天，多出的 {statutoryOff - RequiredStatutoryOffDays} 天由休假轉入，"
                       + $"本月休假日應排 {requiredRest} 天（例假＋休假合計 {requiredOff} 天）。");

        // 休假未排滿只警示、不擋存
        if (restDay < requiredRest)
            warnings.Add($"休假尚未排滿（目前 {restDay} / {requiredRest} 天），可稍後補排。");

        // ── 合併時間軸（本月 + 前後月已定案）──────────────────────────
        var timeline = new SortedDictionary<DateTime, string>();
        if (contextDays is not null)
            foreach (var kv in contextDays) timeline[kv.Key.Date] = kv.Value;
        foreach (var kv in monthDays) timeline[kv.Key.Date] = kv.Value;   // 本月覆蓋 context

        // ── 關卡 A：連續上班不得超過 12 天 ────────────────────────────
        // 只有「上班日」延續連續計數；例假／休假／國定假日皆中斷。
        // 未知的日子（timeline 沒有）同樣中斷 —— 不可當成上班日，否則會誤擋。
        var (maxRun, runEnd) = LongestWorkRun(timeline);
        if (maxRun > MaxConsecutiveWorkDays)
            blocks.Add($"連續出勤不可超過 {MaxConsecutiveWorkDays} 天，請務必排定例假日！"
                     + $"（目前最長連續 {maxRun} 天，至 {runEnd:M/d}）");

        // ── 關卡 B：任意連續 14 天內至少 2 天例假 ─────────────────────
        var violation = FirstRollingWindowViolation(timeline);
        if (violation is { } v)
            blocks.Add($"{v.Start:M/d}～{v.End:M/d} 的 {RollingWindowDays} 天內只有 {v.Count} 天例假日，"
                     + $"依法每 2 週至少需有 {MinStatutoryOffPerWindow} 天，請調整排班。");

        return new ShiftScheduleValidationResult(
            CanSave:              blocks.Count == 0,
            Blocks:               blocks,
            Warnings:             warnings,
            StatutoryOffCount:    statutoryOff,
            RestDayCount:         restDay,
            RequiredStatutoryOff: RequiredStatutoryOffDays,
            RequiredRestDay:      requiredRest,
            RequiredOffDays:      requiredOff);
    }

    /// <summary>最長連續上班日長度與其結束日。日期不連續（中間有未知日）時視為中斷。</summary>
    private static (int Length, DateTime End) LongestWorkRun(SortedDictionary<DateTime, string> timeline)
    {
        int best = 0, run = 0;
        DateTime bestEnd = default, prev = default;

        foreach (var (date, type) in timeline)
        {
            bool contiguous = prev != default && date == prev.AddDays(1);
            run = type == WorkDayTypes.Work ? (contiguous ? run + 1 : 1) : 0;

            if (run > best) { best = run; bestEnd = date; }
            prev = date;
        }
        return (best, bestEnd);
    }

    /// <summary>
    /// 找出第一個違規的滾動 14 天視窗。
    /// **只檢核完整落在已知範圍內的視窗** —— 次月未排定時，跨進未知區的視窗自動略過，
    /// 這正是「次月尚未排定則該側不檢核」的實作。
    /// </summary>
    private static (DateTime Start, DateTime End, int Count)? FirstRollingWindowViolation(
        SortedDictionary<DateTime, string> timeline)
    {
        if (timeline.Count < RollingWindowDays) return null;

        var first = timeline.Keys.First();
        var last  = timeline.Keys.Last();

        for (var start = first; start.AddDays(RollingWindowDays - 1) <= last; start = start.AddDays(1))
        {
            int count = 0;
            bool complete = true;

            for (int i = 0; i < RollingWindowDays; i++)
            {
                if (!timeline.TryGetValue(start.AddDays(i), out var type)) { complete = false; break; }
                if (type == WorkDayTypes.StatutoryOff) count++;
            }

            if (complete && count < MinStatutoryOffPerWindow)
                return (start, start.AddDays(RollingWindowDays - 1), count);
        }
        return null;
    }
}
