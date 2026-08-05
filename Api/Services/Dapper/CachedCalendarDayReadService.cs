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
    private readonly Dictionary<int, HashSet<DateTime>> _holidaysByYear = [];
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

    public async Task<IReadOnlyList<DateTime>> GetHolidayDatesAsync(DateTime startDate, DateTime endDate)
    {
        var s = startDate.Date;
        var e = endDate.Date;
        var result = new List<DateTime>();

        // 以「整年」為單位載入（CalendarDays 本就有 Year 欄位），任何子區間都能就地服務
        for (var y = s.Year; y <= e.Year; y++)
        {
            if (!_holidaysByYear.TryGetValue(y, out var set))
            {
                var dates = await inner.GetHolidayDatesAsync(new DateTime(y, 1, 1), new DateTime(y, 12, 31));
                set = [.. dates.Select(d => d.Date)];
                _holidaysByYear[y] = set;
            }
            result.AddRange(set.Where(d => d >= s && d <= e));
        }

        return [.. result.OrderBy(d => d)];
    }
}
