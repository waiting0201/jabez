using Jabez.Api.Services.Dapper;

namespace Jabez.Api.Common;

/// <summary>
/// 排班月曆的「整月日別組裝」與「國定假日載入」共用實作。
///
/// 規格 §10.4 明訂：月曆讀取（唯讀格）、整月寫入（丟棄該日）、配額重算三個消費點
/// **必須共用同一支 helper**，各寫各的必然漂移。加上〈出勤／排休總覽表〉與活動日改期的重跑檢核，
/// 目前共四個消費點，全部收斂於此。
///
/// ⚠ 「查無紀錄即上班日」是本專案的既定語意：<c>ShiftScheduleDay</c> 只落地非上班日
/// （少寫 ~20 列/人/月）。這與 <see cref="IShiftScheduleReadService"/> 的三段退回**刻意不同** ——
/// 後者服務的是「制度切換前後的執行期判定」（打卡、請假扣假日），沒排班時要退回舊制行事曆；
/// 本 helper 服務的是「這個人這個月排了什麼」，沒排就是沒排，不該替他補上週末＝休假。
/// </summary>
public static class ShiftScheduleMap
{
    /// <summary>
    /// 組出當月每一天的日別：國定假日優先（唯讀、不佔配額）→ 已排的用已排的 → 其餘為上班日。
    /// </summary>
    /// <param name="saved">該員已落地的排班（key 為日期，只含非上班日）。</param>
    /// <param name="publicHolidays">該區間的國定假日（key 為日期、value 為名稱）。</param>
    public static Dictionary<DateTime, string> BuildMonthDayTypes(
        DateTime monthStart, DateTime monthEnd,
        IReadOnlyDictionary<DateTime, string> saved,
        IReadOnlyDictionary<DateTime, string> publicHolidays)
    {
        var map = new Dictionary<DateTime, string>();
        for (var d = monthStart.Date; d <= monthEnd.Date; d = d.AddDays(1))
        {
            map[d] = publicHolidays.ContainsKey(d)   ? WorkDayTypes.PublicHoliday
                   : saved.TryGetValue(d, out var t) ? WorkDayTypes.Normalize(t)
                   :                                   WorkDayTypes.Work;
        }
        return map;
    }

    /// <summary>
    /// 區間內的國定假日 → 名稱。
    ///
    /// ⚠ 必須用 <c>GetByYearAsync</c>（有 <c>Description</c>），**不可用 <c>GetHolidayDatesAsync</c>** ——
    /// 後者只回日期，分不出「國慶日」與「單純的週六」；公司行事曆把週六日也標成 <c>IsHoliday = 1</c>，
    /// 只看旗標會讓員工**永遠排不滿 4 例 4 休**（2026 年 10 月實測：11 天被誤丟，實際國定假日只有 4 天）。
    /// 判準本身收斂在 <see cref="PublicHolidayRule"/>。
    /// </summary>
    public static async Task<Dictionary<DateTime, string>> LoadPublicHolidaysAsync(
        ICalendarDayReadService calendarReader, DateTime from, DateTime to)
    {
        var map = new Dictionary<DateTime, string>();
        for (int y = from.Year; y <= to.Year; y++)
        {
            foreach (var d in await calendarReader.GetByYearAsync(y))
            {
                if (!PublicHolidayRule.IsPublicHoliday(d.IsHoliday, d.Description)) continue;
                if (d.Date.Date < from.Date || d.Date.Date > to.Date) continue;
                map[d.Date.Date] = d.Description;
            }
        }
        return map;
    }
}
