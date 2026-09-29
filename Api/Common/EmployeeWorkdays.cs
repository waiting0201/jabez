using Jabez.Api.Services.Dapper;

namespace Jabez.Api.Common;

/// <summary>
/// 「某一位員工哪幾天算工作日」的判定來源 —— 四週彈性工時切換後，工作日不再是全公司一份行事曆，
/// 而是**每個人自己的排班**（例：排週三休假、週六上班的人，週三請假不該扣假、週六請假才要扣）。
///
/// 判定規則（依**該日自己的日期**與切換日比對，不是今天）：
/// <list type="bullet">
///   <item>切換日之前：舊制 <see cref="WorkCalendarHelper"/>（公司行事曆 + <c>User.IsShiftWorker</c>）。</item>
///   <item>切換日（含）之後：個人日別（<see cref="IShiftScheduleReadService"/>），**只有 <c>work</c> 算工作日**；
///         例假日、休假日、國定假日皆不算。該月尚未排班時由該服務退回舊制行事曆判定。</item>
/// </list>
/// 區間跨切換日時兩段各自判定再合併。
///
/// 消費點（全部必須走這裡，否則同一張假單會在不同畫面算出不同天數）：
///   LeaveRequestHandler（請假日 / 時數 / Submit 擋件）、<see cref="LeaveDayExpander"/>（銷假逐日、排班鎖定、
///   出缺勤請假列、自動補卡）、AttendanceLeaveMerger（應出勤時段 + 缺勤列）、AttendanceAutoClockService（補上班卡）。
///
/// 新制段的 <see cref="CalendarScope"/> 不影響結果：彈性休假日在新制是可排班日，上不上班看個人排班，
/// 故出勤語意與請假語意的答案一致。
///
/// ⚠ 已知限制：請假送出後，若再把假期間內的休假日改排為上班日（或反之），逐日展開會跟著改變，
/// 與送簽當下存入的 <c>LeaveRequest.Hours</c> 不一致。已請假的「工作日」由排班鎖定擋住不能改為休假，
/// 但反方向（休假改上班）目前不擋。
/// </summary>
public sealed class EmployeeWorkdays(
    ICalendarDayReadService calendarReader,
    bool isShiftWorker,
    DateTime? switchDate,
    Func<DateTime, DateTime, Task<IReadOnlyDictionary<DateTime, string>>>? personalDayTypes)
{
    /// <summary>
    /// 僅舊制判定（不看個人排班）。供不涉及個人的場合，或切換日確定為 null 時使用。
    /// </summary>
    public static EmployeeWorkdays Legacy(ICalendarDayReadService calendarReader, bool isShiftWorker) =>
        new(calendarReader, isShiftWorker, null, null);

    /// <summary>該日適用的工作時段（舊制 08:00–17:00 / 新制 09:00–18:00）。</summary>
    public WorkdaySchedule ScheduleFor(DateTime date) => WorkdayHours.For(date, switchDate);

    /// <summary>計算 [start, end] 內的工作日 / 非工作日清單。hasData＝區間橫跨的年度行事曆皆已匯入。</summary>
    public async Task<(bool hasData, List<DateTime> holidays, List<DateTime> working)>
        ComputeAsync(DateTime start, DateTime end, CalendarScope scope)
    {
        var s = start.Date;
        var e = end.Date;

        if (switchDate is not { } sw || personalDayTypes is null || e < sw.Date)
            return await WorkCalendarHelper.ComputeWorkingDatesAsync(calendarReader, isShiftWorker, s, e, scope);

        var hasData  = true;
        var holidays = new List<DateTime>();
        var working  = new List<DateTime>();

        // 切換日之前的部分：舊制
        if (s < sw.Date)
        {
            var legacy = await WorkCalendarHelper.ComputeWorkingDatesAsync(
                calendarReader, isShiftWorker, s, sw.Date.AddDays(-1), scope);
            hasData = legacy.hasData;
            holidays.AddRange(legacy.holidays);
            working.AddRange(legacy.working);
            s = sw.Date;
        }

        // 切換日之後：個人排班。國定假日仍來自行事曆，未匯入的年度照樣回報 hasData=false（Submit 會擋）
        hasData &= await WorkCalendarHelper.HasCalendarForAllYearsAsync(calendarReader, ignoreHolidays: false, s, e);

        var types = await personalDayTypes(s, e);
        foreach (var d in WorkCalendarHelper.EnumerateDates(s, e))
        {
            // 解析服務對明確指定的員工必回每一天；萬一缺值視為上班日（與「查無紀錄即上班日」一致）
            var isWork = !types.TryGetValue(d, out var t) || t == WorkDayTypes.Work;
            if (isWork) working.Add(d); else holidays.Add(d);
        }
        return (hasData, holidays, working);
    }
}
