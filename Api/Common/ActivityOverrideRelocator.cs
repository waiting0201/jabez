namespace Jabez.Api.Common;

/// <param name="Month">覆蓋並補排後的當月日別（每一天，含國定假日）。</param>
/// <param name="RelocatedTo">原例假／休假搬到的日子；null ＝ 找不到合法的日子（待補排）。</param>
public sealed record ActivityOverrideResult(
    IReadOnlyDictionary<DateTime, string> Month,
    DateTime? RelocatedTo);

/// <summary>
/// 活動日覆蓋個人排班時的「補排」（2026-09-29 決議：活動日優先）。純函式、無 I/O，比照 <see cref="AutoShiftScheduler"/>。
///
/// 主管把活動日排在同仁已排定的例假／休假上 → 該日強制改為上班日，少掉的那一天搬到當月另一個日子。
/// **例假不能只蓋掉就好**：當月例假會少於 4 天、或違反「任意 14 天內 ≥2 例假」，班表即違法。
///
/// <b>規則</b>：
/// <list type="number">
///   <item>一次只搬一天、不重排整月 —— 盡量保留同仁自己排的班</item>
///   <item>候選日＝當月的上班日，扣掉 <c>blockedDates</c>（過去日、國定假日、本人的活動日與請假日、
///         被進行中改班單佔用的日子）與衝突日本身</item>
///   <item>依「離衝突日最近 → 同距離取較晚」逐一試排，第一個讓 <see cref="ShiftScheduleValidator"/> 通過的即採用
///         （覆蓋前本就不合規的暫存班表，改以「擋存原因不比覆蓋前多」為準）</item>
///   <item>都不通過 → 回傳 <c>RelocatedTo = null</c>（待補排），此時只把衝突日改成上班日 ——
///         **不可硬塞一個仍違法的日子**，那會讓同仁以為系統已經處理好了</item>
/// </list>
/// </summary>
public static class ActivityOverrideRelocator
{
    /// <param name="monthDays">覆蓋前的當月日別（<see cref="ShiftScheduleMap.BuildMonthDayTypes"/> 的結果）。</param>
    /// <param name="conflictDate">活動日（目前排為例假／休假）。</param>
    /// <param name="blockedDates">不可拿來補排的日子。</param>
    /// <param name="contextDays">前後月已定案班表，供關卡 A／B 跨月檢核（見 <see cref="ShiftScheduleValidator.Validate"/>）。</param>
    public static ActivityOverrideResult Relocate(
        int year, int month,
        IReadOnlyDictionary<DateTime, string> monthDays,
        DateTime conflictDate,
        IReadOnlySet<DateTime> blockedDates,
        IReadOnlyDictionary<DateTime, string>? contextDays = null)
    {
        conflictDate = conflictDate.Date;
        var lostType = monthDays[conflictDate];

        var map = new Dictionary<DateTime, string>(monthDays) { [conflictDate] = WorkDayTypes.Work };

        var candidates = map
            .Where(kv => kv.Value == WorkDayTypes.Work
                      && kv.Key != conflictDate
                      && !blockedDates.Contains(kv.Key))
            .Select(kv => kv.Key)
            .OrderBy(d => Math.Abs((d - conflictDate).TotalDays))
            .ThenByDescending(d => d)
            .ToList();

        // 覆蓋前的班表本身就可能還沒排完（開放期內的暫存），這時要求「補排後全過」永遠做不到。
        // 改以「不比覆蓋前更糟」為準：原本合規 → 補排後須合規；原本不合規 → 擋存原因不得變多。
        var before = ShiftScheduleValidator.Validate(year, month, monthDays, contextDays);

        foreach (var d in candidates)
        {
            map[d] = lostType;
            var after = ShiftScheduleValidator.Validate(year, month, map, contextDays);
            if (after.CanSave || (!before.CanSave && after.Blocks.Count <= before.Blocks.Count))
                return new ActivityOverrideResult(map, d);
            map[d] = WorkDayTypes.Work;
        }

        return new ActivityOverrideResult(map, null);
    }
}
