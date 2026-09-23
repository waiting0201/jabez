using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

/// <summary>
/// 單次讀取作業內的行事曆記憶體快取（decorator）。
///
/// LeaveDayExpander 逐張假單會呼叫 HasDataForRangeAsync（逐年）+ GetHolidayDatesAsync，
/// N 張假單 = 2N+ 次 DB round-trip；此包裝以「年」為粒度收斂成每個年度最多 2 次。
///
/// 刻意不註冊進 DI：只在唯讀合併流程（AttendanceLeaveMerger）中 new，
/// 避免與同請求內的行事曆寫入（CalendarDayHandler）產生陳舊快取。
/// </summary>
public sealed class CachedCalendarDayReadService(ICalendarDayReadService inner) : ICalendarDayReadService
{
    // ⚠ 依 excludeFlexibleHoliday 分兩個 slot：同一次合併流程裡「請假語意」與「出勤語意」都會查，
    //    共用一份的話先查到的那種語意會被另一種重用（彈性休假日會多算 / 少算一天，且畫面看不出異常）
    private readonly Dictionary<(int Year, bool ExcludeFlexible), HashSet<DateTime>> _holidaysByYear = [];
    private readonly Dictionary<(DateTime Start, DateTime End), bool> _hasData = [];

    public Task<IEnumerable<CalendarDayDto>> GetByYearAsync(int year) => inner.GetByYearAsync(year);

    public Task<int> CountHolidaysAsync(DateTime startDate, DateTime endDate) =>
        inner.CountHolidaysAsync(startDate, endDate);

    public async Task<bool> HasDataForRangeAsync(DateTime startDate, DateTime endDate)
    {
        // WorkCalendarHelper.HasCalendarForAllYearsAsync 恆以 (y/1/1, y/12/31) 呼叫 → 命中率 100%
        var key = (startDate.Date, endDate.Date);
        if (_hasData.TryGetValue(key, out var cached)) return cached;

        var value = await inner.HasDataForRangeAsync(startDate, endDate);
        _hasData[key] = value;
        return value;
    }

    public async Task<IReadOnlyList<DateTime>> GetHolidayDatesAsync(
        DateTime startDate, DateTime endDate, bool excludeFlexibleHoliday = false)
    {
        var s = startDate.Date;
        var e = endDate.Date;
        var result = new List<DateTime>();

        // 以「整年」為單位載入（CalendarDays 本就有 Year 欄位），任何子區間都能就地服務
        for (var y = s.Year; y <= e.Year; y++)
        {
            var key = (y, excludeFlexibleHoliday);
            if (!_holidaysByYear.TryGetValue(key, out var set))
            {
                var dates = await inner.GetHolidayDatesAsync(
                    new DateTime(y, 1, 1), new DateTime(y, 12, 31), excludeFlexibleHoliday);
                set = [.. dates.Select(d => d.Date)];
                _holidaysByYear[key] = set;
            }
            result.AddRange(set.Where(d => d >= s && d <= e));
        }

        return [.. result.OrderBy(d => d)];
    }
}
