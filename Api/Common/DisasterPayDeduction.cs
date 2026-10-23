namespace Jabez.Api.Common;

/// <summary>某位員工在某一個颱風假日的出勤事實（由 DisasterPayrollService 組好後傳入純函式）。</summary>
/// <param name="Date">停班日</param>
/// <param name="Segment">full / am / pm</param>
/// <param name="IsPaid">該筆颱風假是否給薪</param>
/// <param name="IsWorkingDay">是否為本人當日的工作日（個人排班 / 排班制旗標 / 行事曆；到職前、離職後為 false）</param>
/// <param name="WorkedInSegment">打卡區間與停班時段重疊（有出勤）</param>
/// <param name="HasApprovedLeaveInSegment">停班時段內已有核准請假（任何重疊皆算，偏向對員工有利）</param>
public sealed record DisasterPayDay(
    DateTime Date, string Segment, bool IsPaid, bool IsWorkingDay,
    bool WorkedInSegment, bool HasApprovedLeaveInSegment);

/// <summary>
/// 不給薪颱風假的扣薪天數 / 金額（純函式，無 I/O，可單元測試）。
///
/// 規則（見 docs/business/disaster-day-off.md）：
///   · 只有「不給薪」的颱風假才可能扣薪；給薪者一律不扣。
///   · 必須是本人當日的工作日（排班例休、到職前、離職後不扣）。
///   · 停班時段內有出勤（打卡重疊）→ 照平日、不扣。
///   · 停班時段內已有核准請假 → 假單照原假別計薪（事假／病假已在別處扣），不另扣。
///   · 其餘（未出勤、未請假的工作日）→ 全天 1 日、半天 0.5 日，× 日薪（折減前的底薪 ÷ 30）。
/// 整月育嬰留停被剔除薪資單者，由呼叫端略過，不進此函式。
/// </summary>
public static class DisasterPayDeduction
{
    /// <summary>扣薪天數合計。</summary>
    public static decimal DeductibleDays(IEnumerable<DisasterPayDay> days) =>
        days.Where(IsDeductible).Sum(d => DisasterSegments.Days(d.Segment));

    public static bool IsDeductible(DisasterPayDay d) =>
        !d.IsPaid && d.IsWorkingDay && !d.WorkedInSegment && !d.HasApprovedLeaveInSegment;

    /// <summary>扣薪金額＝日薪 × 天數，四捨五入至整數元（與假日津貼同樣 AwayFromZero，避免奇數日薪 × 0.5 少 1 元）。</summary>
    public static decimal Amount(decimal dailySalary, decimal days) =>
        Math.Round(dailySalary * days, 0, MidpointRounding.AwayFromZero);

    /// <summary>
    /// 打卡區間是否與停班時段重疊。只有上班卡、尚未下班 ＝ 區間視為延伸到當日結束（人在上班中）；
    /// 沒有上班卡 ＝ 未出勤（只有加班時間的紀錄不算）。
    /// </summary>
    public static bool ClockOverlapsSegment(DateTime? clockIn, DateTime? clockOut, DateTime segStart, DateTime segEnd)
    {
        if (clockIn is null) return false;
        var end = clockOut ?? segEnd.Date.AddDays(1);
        return clockIn.Value < segEnd && end > segStart;
    }
}

/// <summary>
/// 給薪颱風假對請假時數的退回計算（純函式）：某日請假被停班時段蓋住的部分，不再算請假。
/// </summary>
public static class DisasterLeaveAdjuster
{
    /// <summary>
    /// 套用停班時段後，這一天剩餘的請假時數。
    /// 全天停班 → 0；上午 / 下午停班 → 該半天部分扣掉（整日請假 8h → 4h、同半天的半天假 → 0、另一半天的半天假不變）；
    /// 小時假（partial）依與停班時段的重疊小時扣除（沿用既有「整點差、不扣午休」語意）。
    /// </summary>
    public static decimal RemainingHours(LeaveDay day, string disasterSegment, WorkdaySchedule sch)
    {
        if (disasterSegment == DisasterSegments.Full) return 0m;

        var wStart = disasterSegment == DisasterSegments.Am ? sch.Start : sch.HalfDayPmStart;
        var wEnd   = disasterSegment == DisasterSegments.Am ? sch.HalfDayAmEnd : sch.End;

        switch (day.Segment)
        {
            case LeaveDaySegments.Full:
                return Math.Max(0m, day.Hours - WorkdaySchedule.HalfDayHours);
            case LeaveDaySegments.Am:
                return disasterSegment == DisasterSegments.Am ? 0m : day.Hours;
            case LeaveDaySegments.Pm:
                return disasterSegment == DisasterSegments.Pm ? 0m : day.Hours;
            default:
            {
                var lo = day.Start > wStart ? day.Start : wStart;
                var hi = day.End   < wEnd   ? day.End   : wEnd;
                var overlap = hi > lo ? (decimal)(hi - lo).TotalHours : 0m;
                return Math.Max(0m, day.Hours - Math.Round(overlap, 1));
            }
        }
    }

    /// <summary>
    /// 多日請假（已排除銷假日）在給薪颱風假下的總退回時數。
    /// <paramref name="paidSegments"/>：日期 → 該員當日的給薪停班時段（僅含給薪者）。
    /// </summary>
    public static decimal TotalReduction(
        IEnumerable<LeaveDay> days,
        IReadOnlyDictionary<DateTime, string> paidSegments,
        Func<DateTime, WorkdaySchedule> scheduleFor)
    {
        decimal reduction = 0m;
        foreach (var d in days)
            if (paidSegments.TryGetValue(d.Date.Date, out var seg))
                reduction += d.Hours - RemainingHours(d, seg, scheduleFor(d.Date));
        return reduction;
    }
}
