using Jabez.Api.Services.Dapper;

namespace Jabez.Api.Common;

/// <summary>
/// 公司行事曆（CalendarDay）共用判定 —— 「行事曆有資料用 IsHoliday、沒資料退回六日」的單一真相。
/// 行事曆有資料 → 以 CalendarDay.IsHoliday（已含六日 + 國定假、補班六為工作日）為準；
/// 無資料 → 退回以星期六日判定（國定假需匯入行事曆才會扣）。
///
/// **排班制員工（User.IsShiftWorker，賣店 / 營業所）**：六日與國定假日照常營業，皆為工作日。
/// 三個方法的 <c>ignoreHolidays</c> 參數為 true 時直接短路（完全不查行事曆），
/// 使其能正常請六日的假。此參數**刻意必填、不給預設值** —— 新增消費點時漏傳會是編譯錯誤，
/// 而不是讓某人的請假天數被靜默算成 0。
///
/// 消費點：
///   LeaveRequestHandler → 請假日清單 / Hour 單位時數 / Submit 擋件（區間版）
///   AttendanceHandler   → 休假日免下班卡即可打「加班開始」（單日版）
///   AttendanceReminderService → 週末只提醒排班制員工（單日版）
///
/// 旗標一律以「假單所有人 / 打卡本人」解析（見 IWorkPatternReadService），不可用呼叫者 id。
/// </summary>
public static class WorkCalendarHelper
{
    public static IEnumerable<DateTime> EnumerateDates(DateTime start, DateTime end)
    {
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            yield return d;
    }

    /// <summary>計算 [start, end] 內的請假日 / 假日清單。</summary>
    /// <param name="ignoreHolidays">排班制員工：整段皆為工作日，不查行事曆。</param>
    public static async Task<(bool hasData, List<DateTime> holidays, List<DateTime> working)>
        ComputeWorkingDatesAsync(ICalendarDayReadService calendarReader, bool ignoreHolidays, DateTime start, DateTime end)
    {
        var s = start.Date;
        var e = end.Date;

        // 排班制：全部日期皆為工作日。hasData 回 true 讓 Submit 不會因「尚未匯入行事曆」被擋。
        if (ignoreHolidays)
            return (true, [], [.. EnumerateDates(s, e)]);

        var hasData = await HasCalendarForAllYearsAsync(calendarReader, ignoreHolidays, s, e);
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
    /// 排班制員工不看行事曆，恆為 true。
    /// </summary>
    public static async Task<bool> HasCalendarForAllYearsAsync(
        ICalendarDayReadService calendarReader, bool ignoreHolidays, DateTime start, DateTime end)
    {
        if (ignoreHolidays) return true;

        for (var y = start.Year; y <= end.Year; y++)
        {
            if (!await calendarReader.HasDataForRangeAsync(new DateTime(y, 1, 1), new DateTime(y, 12, 31)))
                return false;
        }
        return true;
    }

    /// <summary>單日版：指定日是否為休假日（行事曆優先，該年度無資料時退回六日）。排班制員工恆為 false。</summary>
    public static async Task<bool> IsHolidayAsync(
        ICalendarDayReadService calendarReader, bool ignoreHolidays, DateTime date)
    {
        if (ignoreHolidays) return false;

        var d = date.Date;
        if (!await HasCalendarForAllYearsAsync(calendarReader, ignoreHolidays, d, d))
            return d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        return (await calendarReader.GetHolidayDatesAsync(d, d)).Count > 0;
    }
}
