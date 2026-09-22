namespace Jabez.Api.Common;

/// <summary>
/// 某日的「應出勤（可打卡）時段」。
/// Start / End 為 null＝當日完全免出勤（全日請假，或上下午各一張半天假把整天蓋滿）。
/// </summary>
/// <param name="Start">應出勤起（含日期）</param>
/// <param name="End">應出勤訖（含日期）</param>
/// <param name="StartAdjustedByLeave">起點因請假而後延（原本是該日時段的上班時刻）</param>
/// <param name="EndAdjustedByLeave">訖點因請假而提前（原本是該日時段的下班時刻）</param>
public readonly record struct WorkWindow(
    DateTime? Start, DateTime? End, bool StartAdjustedByLeave, bool EndAdjustedByLeave);

/// <summary>
/// 「該日應出勤時段」的單一真相（純函式、無 I/O，比照 <see cref="OvertimePayCalculator"/>）。
///
/// 以該日適用的工作日標準時段為起點（舊制 08:00–17:00、四週彈性工時 09:00–18:00，
/// 由 <see cref="WorkdayHours.For"/> 依 SystemSetting.FlexibleWorkStartDate 與**該日日期**選用），
/// 被當日已核准請假蓋掉的頭尾往內縮（以下以舊制為例）：
///   上午請假 08:00–12:00 → 13:00–17:00（跨午休正規化，不會算成 12:00 開工）
///   下午請假 13:00–17:00 → 08:00–12:00
///   中段小時假 10:00–12:00 → 維持 08:00–17:00（單一區間表達不了中間挖洞，刻意不縮）
///
/// 消費點：
///   AttendanceLeaveMerger        → 出缺勤報表的應出勤欄與「未打卡 / 缺勤」判定
///   AttendanceAutoClockService   → 登入自動補卡的時間（補的卡不得落在請假時段內）
///
/// <para>
/// <b>兩個 AdjustedByLeave 旗標是必要的，不可改用「值是否等於 08:00 / 17:00」推斷。</b>
/// 補下班卡的既有行為是「上班打卡 + 9 小時」（早到晚到者的工時才不會失真，見 AuthHandler），
/// 無請假時 End 恆為該日時段的下班時刻，若無條件取 min(上班+9h, End) 會把 09:00 上班者從 18:00 壓成 17:00，
/// 推翻 2026-08 刻意做的決策。故呼叫端一律以 EndAdjustedByLeave 為閘門。
/// </para>
/// </summary>
public static class ExpectedWorkWindow
{
    /// <summary>
    /// 計算指定日期的應出勤時段。
    /// </summary>
    /// <param name="date">目標日期（只取日期部分）</param>
    /// <param name="dayLeaves">該日的請假逐日展開結果（<see cref="LeaveDayExpander.ExpandAsync"/> 產出，需為同一天）</param>
    /// <param name="schedule">
    /// 該日適用的工作時段。傳 null ＝ 舊制（等同四週彈性工時上線前的行為），
    /// 供尚未接上切換日的呼叫點暫時沿用；接上後一律傳 <c>WorkdayHours.For(date, switchDate)</c>。
    /// </param>
    public static WorkWindow Compute(DateTime date, IReadOnlyList<LeaveDay> dayLeaves, WorkdaySchedule? schedule = null)
    {
        var s          = schedule ?? WorkdayHours.Legacy;
        var day        = date.Date;
        var start      = s.Start;       // 舊制 08:00 / 新制 09:00
        var end        = s.End;         // 舊制 17:00 / 新制 18:00
        var lunchStart = s.LunchStart;  // 舊制 12:00 / 新制 12:30
        var lunchEnd   = s.LunchEnd;    // 舊制 13:00 / 新制 13:30

        bool startAdjusted = false;
        bool endAdjusted   = false;

        if (dayLeaves.Count > 0)
        {
            // 起點：落在某段假內就往後推到該段結束（半開區間 [Start, End)，與 EnsureNotOnLeaveAsync 同慣例）。
            // 迴圈上限＝假段數 + 1，防資料異常導致無限迴圈。
            for (int i = 0; i <= dayLeaves.Count; i++)
            {
                int hit = IndexOfCovering(dayLeaves, seg => seg.Start <= start && start < seg.End);
                if (hit < 0) break;

                start = dayLeaves[hit].End;
                startAdjusted = true;

                // 跨午休正規化：上午請假到 12:00 → 實際 13:00 才開工
                if (start >= lunchStart && start < lunchEnd) start = lunchEnd;
                if (start >= end) break;
            }

            // 訖點：落在某段假內就往前縮到該段開始（右閉，17:00 落在 13:00–17:00 內要算命中）
            for (int i = 0; i <= dayLeaves.Count; i++)
            {
                int hit = IndexOfCovering(dayLeaves, seg => seg.Start < end && end <= seg.End);
                if (hit < 0) break;

                end = dayLeaves[hit].Start;
                endAdjusted = true;

                // 跨午休正規化：下午請假從 13:00 起 → 實際 12:00 就下班
                if (end > lunchStart && end <= lunchEnd) end = lunchStart;
                if (end <= start) break;
            }
        }

        // 整天被請假蓋滿 → 當日免出勤
        if (start >= end) return new WorkWindow(null, null, true, true);

        return new WorkWindow(
            day.Add(start.ToTimeSpan()),
            day.Add(end.ToTimeSpan()),
            startAdjusted,
            endAdjusted);
    }

    private static int IndexOfCovering(IReadOnlyList<LeaveDay> leaves, Func<LeaveDay, bool> covers)
    {
        for (int i = 0; i < leaves.Count; i++)
            if (covers(leaves[i])) return i;
        return -1;
    }
}
