namespace Jabez.Api.Common;

/// <param name="Success">是否排出合法班表。false 時 <see cref="Days"/> 為空 —— **絕不寫入半成品**。</param>
/// <param name="Days">排定結果（只含非上班日；key 為日期）</param>
/// <param name="FailureReason">失敗原因，供通知部門協理與行政部門人工處理</param>
/// <param name="Attempts">遞補次數，供觀察演算法是否吃力</param>
public sealed record AutoScheduleResult(
    bool Success,
    IReadOnlyDictionary<DateTime, string> Days,
    string? FailureReason,
    int Attempts);

/// <summary>
/// 逾期未排班者的系統自動排班（四週彈性工時 §3.5.1）。純函式、無 I/O。
///
/// <b>設計原則</b>：唯一的硬性要求是**產出必須合於《勞基法》**（例假 4 天、連續上班 ≤ 12 天、
/// 任意 14 天內 ≥ 2 例假）。規則刻意做得單純可驗證 —— 它只是逾期者的保底，
/// 真正要排得好的人請在開放期內自己排。
///
/// <b>規則</b>：
/// <list type="number">
///   <item>該月每一天預設為上班日；**國定假日**維持唯讀、不參與、不佔配額</item>
///   <item>候選日優先序：**週日 → 週六 → 其餘平日**（各組內由月初往後依序）</item>
///   <item>依序取 4 天排為例假日，再依序取 4 天（31 日曆月 5 天）排為休假日</item>
///   <item>**主管已排定的活動日一律跳過**，不會被排成例假或休假</item>
///   <item>排完跑三條檢核，任一不過就**往後遞補** —— 把最接近違規區間的候選日改排為例假日，重跑至全過</item>
///   <item>⚠ **候選日用盡仍無法通過時，一律不得產生違法班表**：回傳失敗、班表留白不寫入，
///         交由部門協理與行政部門人工處理</item>
/// </list>
/// </summary>
public static class AutoShiftScheduler
{
    /// <summary>遞補次數上限。超過視為排不出來 —— 正常月份 2～3 次就會收斂。</summary>
    private const int MaxAttempts = 40;

    /// <param name="publicHolidays">國定假日（不參與、不佔配額）</param>
    /// <param name="activityDays">主管已排定的活動日（一律跳過）</param>
    /// <param name="contextDays">
    /// 前月月底與次月月初的已定案班表，供關卡 A／B 跨月檢核；次月未排定就不要放。
    /// </param>
    public static AutoScheduleResult Build(
        int year, int month,
        IReadOnlySet<DateTime> publicHolidays,
        IReadOnlySet<DateTime> activityDays,
        IReadOnlyDictionary<DateTime, string>? contextDays = null)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);

        // 候選日＝當月扣掉國定假日與活動日，依「週日 → 週六 → 平日」分組、組內由月初往後
        var candidates = EnumerateDates(monthStart, monthEnd)
            .Where(d => !publicHolidays.Contains(d) && !activityDays.Contains(d))
            .OrderBy(d => d.DayOfWeek switch
            {
                DayOfWeek.Sunday   => 0,
                DayOfWeek.Saturday => 1,
                _                  => 2,
            })
            .ThenBy(d => d)
            .ToList();

        int requiredRest = ShiftScheduleValidator.RequiredRestDaysFor(year, month);
        int requiredOff  = ShiftScheduleValidator.RequiredStatutoryOffDays;

        if (candidates.Count < requiredOff + requiredRest)
            return new AutoScheduleResult(false, new Dictionary<DateTime, string>(),
                $"可排定的日子只有 {candidates.Count} 天，不足以排滿 {requiredOff} 天例假與 {requiredRest} 天休假"
                + "（該月國定假日與活動日過多），請人工處理。", 0);

        var assigned = new Dictionary<DateTime, string>();
        foreach (var d in candidates.Take(requiredOff))
            assigned[d] = WorkDayTypes.StatutoryOff;
        foreach (var d in candidates.Skip(requiredOff).Take(requiredRest))
            assigned[d] = WorkDayTypes.RestDay;

        // 遞補：把最接近違規區間的候選日改排為例假日，重跑檢核
        for (int attempt = 0; attempt <= MaxAttempts; attempt++)
        {
            var map = BuildFullMap(monthStart, monthEnd, assigned, publicHolidays);
            var check = ShiftScheduleValidator.Validate(year, month, map, contextDays);

            if (check.CanSave)
                return new AutoScheduleResult(true, assigned, null, attempt);

            var fix = FindFixDate(monthStart, monthEnd, map, assigned, publicHolidays, activityDays);
            if (fix is null)
                return new AutoScheduleResult(false, new Dictionary<DateTime, string>(),
                    "候選日已用盡仍無法排出合法班表（" + string.Join("；", check.Blocks) + "），請人工處理。",
                    attempt);

            // 若動到的是休假日，等於把休假挪去補例假 —— 之後要從剩餘候選日補回一天休假，
            // 否則遞補幾次就會把休假配額吃光（只會出警示，但同仁實際少放假）。
            bool tookRestDay = assigned.TryGetValue(fix.Value, out var prev) && prev == WorkDayTypes.RestDay;
            assigned[fix.Value] = WorkDayTypes.StatutoryOff;

            if (tookRestDay)
            {
                var spare = candidates.FirstOrDefault(d => !assigned.ContainsKey(d));
                if (spare != default) assigned[spare] = WorkDayTypes.RestDay;
            }
        }

        return new AutoScheduleResult(false, new Dictionary<DateTime, string>(),
            $"遞補 {MaxAttempts} 次仍無法排出合法班表，請人工處理。", MaxAttempts);
    }

    /// <summary>
    /// 找一個「改排為例假日」最有幫助的日子。
    ///
    /// 優先修**連續上班過長**（把最長連續區間的中點改為例假，一刀切兩半最有效），
    /// 其次補**14 天視窗例假不足**（在該視窗內找一個上班日）。
    /// 都找不到可動的日子時回 null ＝ 候選日用盡。
    /// </summary>
    private static DateTime? FindFixDate(
        DateTime monthStart, DateTime monthEnd,
        IReadOnlyDictionary<DateTime, string> map,
        IReadOnlyDictionary<DateTime, string> assigned,
        IReadOnlySet<DateTime> publicHolidays,
        IReadOnlySet<DateTime> activityDays)
    {
        bool Movable(DateTime d) =>
            d >= monthStart && d <= monthEnd
            && !publicHolidays.Contains(d)
            && !activityDays.Contains(d)
            && (!assigned.TryGetValue(d, out var t) || t != WorkDayTypes.StatutoryOff);

        // 優先動「上班日」，真的沒有才動休假日 —— 動休假等於少放一天假，能不動就不動
        bool IsWork(DateTime d) => !assigned.ContainsKey(d);

        // ① 最長連續上班區間 → 取中點
        var (runStart, runLength) = LongestWorkRun(monthStart, monthEnd, map);
        if (runLength > ShiftScheduleValidator.MaxConsecutiveWorkDays)
        {
            var mid = runStart.AddDays(runLength / 2);
            for (int offset = 0; offset < runLength; offset++)
            {
                // 由中點往兩側找第一個可動的日子
                if (Movable(mid.AddDays(offset)) && mid.AddDays(offset) <= runStart.AddDays(runLength - 1))
                    return mid.AddDays(offset);
                if (Movable(mid.AddDays(-offset)) && mid.AddDays(-offset) >= runStart)
                    return mid.AddDays(-offset);
            }
        }

        // ② 第一個例假不足的 14 天視窗 → 取視窗內第一個可動的日子
        for (var start = monthStart;
             start.AddDays(ShiftScheduleValidator.RollingWindowDays - 1) <= monthEnd;
             start = start.AddDays(1))
        {
            int count = 0;
            for (int i = 0; i < ShiftScheduleValidator.RollingWindowDays; i++)
                if (map.TryGetValue(start.AddDays(i), out var t) && t == WorkDayTypes.StatutoryOff) count++;

            if (count >= ShiftScheduleValidator.MinStatutoryOffPerWindow) continue;

            // ⚠ **由視窗尾端往前找**，不可從頭找。
            // 滾動視窗每次往後移一天，補在開頭的例假馬上就掉出下一個視窗，
            // 於是每個視窗都要再補一次 —— 實測會從 4 天例假一路補到 11 天（連續補 7 天）。
            // 補在尾端則能同時涵蓋最多個後續視窗，通常一兩次就收斂。
            for (int i = ShiftScheduleValidator.RollingWindowDays - 1; i >= 0; i--)
                if (Movable(start.AddDays(i)) && IsWork(start.AddDays(i))) return start.AddDays(i);
            for (int i = ShiftScheduleValidator.RollingWindowDays - 1; i >= 0; i--)
                if (Movable(start.AddDays(i))) return start.AddDays(i);
        }

        // ③ 例假仍不足 4 天 → 任一可動的日子
        return EnumerateDates(monthStart, monthEnd).FirstOrDefault(Movable) is { } any
               && Movable(any) ? any : null;
    }

    private static (DateTime Start, int Length) LongestWorkRun(
        DateTime monthStart, DateTime monthEnd, IReadOnlyDictionary<DateTime, string> map)
    {
        int best = 0, run = 0;
        DateTime bestStart = monthStart, runStart = monthStart;

        foreach (var d in EnumerateDates(monthStart, monthEnd))
        {
            if (map.TryGetValue(d, out var t) && t == WorkDayTypes.Work)
            {
                if (run == 0) runStart = d;
                run++;
                if (run > best) { best = run; bestStart = runStart; }
            }
            else run = 0;
        }
        return (bestStart, best);
    }

    private static Dictionary<DateTime, string> BuildFullMap(
        DateTime monthStart, DateTime monthEnd,
        IReadOnlyDictionary<DateTime, string> assigned,
        IReadOnlySet<DateTime> publicHolidays)
    {
        var holidayMap = publicHolidays.ToDictionary(d => d, _ => WorkDayTypes.PublicHoliday);
        return ShiftScheduleMap.BuildMonthDayTypes(monthStart, monthEnd, assigned, holidayMap);
    }

    private static IEnumerable<DateTime> EnumerateDates(DateTime start, DateTime end)
    {
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1)) yield return d;
    }
}
