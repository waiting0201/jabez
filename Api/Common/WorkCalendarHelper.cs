using Jabez.Api.Services.Dapper;

namespace Jabez.Api.Common;

/// <summary>
/// 公司行事曆（CalendarDay）共用判定 —— 「行事曆有資料用 IsHoliday、沒資料退回六日」的單一真相。
/// 行事曆有資料 → 以 CalendarDay.IsHoliday（已含六日 + 國定假、補班六為工作日）為準；
/// 無資料 → 退回以星期六日判定（國定假需匯入行事曆才會扣）。
///
/// 消費點：
///   LeaveRequestHandler → 請假日清單 / Hour 單位時數 / Submit 擋件（區間版）
///   AttendanceHandler   → 休假日免下班卡即可打「加班開始」（單日版）
/// </summary>
public static class WorkCalendarHelper
{
    public static IEnumerable<DateTime> EnumerateDates(DateTime start, DateTime end)
    {
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            yield return d;
    }

    /// <summary>計算 [start, end] 內的請假日 / 假日清單。</summary>
    public static async Task<(bool hasData, List<DateTime> holidays, List<DateTime> working)>
        ComputeWorkingDatesAsync(ICalendarDayReadService calendarReader, DateTime start, DateTime end)
    {
        var s = start.Date;
        var e = end.Date;
        var hasData = await HasCalendarForAllYearsAsync(calendarReader, s, e);
        var holidaySet = hasData
            ? (await calendarReader.GetHolidayDatesAsync(s, e)).Select(d => d.Date).ToHashSet()
            : [];

        var holidays = new List<DateTime>();
        var working  = new List<DateTime>();
        foreach (var d in EnumerateDates(s, e))
        {
            bool isHoliday = hasData
                ? holidaySet.Contains(d)
                : d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            if (isHoliday) holidays.Add(d); else working.Add(d);
        }
        return (hasData, holidays, working);
    }

    /// <summary>
    /// 檢查區間橫跨的「每一個年度」都已匯入行事曆。
    /// CalendarDayReadService.HasDataForRangeAsync 是 EXISTS 語意（區間內任一天有資料即為 true），
    /// 產假（56 個日曆天）與拉長後的婚假 / 喪假可能跨年，只匯入其中一年會誤判，故逐年檢查。
    /// </summary>
    public static async Task<bool> HasCalendarForAllYearsAsync(
        ICalendarDayReadService calendarReader, DateTime start, DateTime end)
    {
        for (var y = start.Year; y <= end.Year; y++)
        {
            if (!await calendarReader.HasDataForRangeAsync(new DateTime(y, 1, 1), new DateTime(y, 12, 31)))
                return false;
        }
        return true;
    }

    /// <summary>單日版：指定日是否為休假日（行事曆優先，該年度無資料時退回六日）。</summary>
    public static async Task<bool> IsHolidayAsync(ICalendarDayReadService calendarReader, DateTime date)
    {
        var d = date.Date;
        if (!await HasCalendarForAllYearsAsync(calendarReader, d, d))
            return d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        return (await calendarReader.GetHolidayDatesAsync(d, d)).Count > 0;
    }
}
