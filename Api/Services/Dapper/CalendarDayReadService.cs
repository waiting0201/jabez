using System.Data;
using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface ICalendarDayReadService
{
    Task<IEnumerable<CalendarDayDto>> GetByYearAsync(int year);
    Task<int> CountHolidaysAsync(DateTime startDate, DateTime endDate);
    Task<bool> HasDataForRangeAsync(DateTime startDate, DateTime endDate);
    /// <param name="excludeFlexibleHoliday">
    /// true＝排除「彈性休假日」（原行事曆的「補假」）。**僅請假日判定會傳 true**：那些日子仍是休假日
    /// （不用上班、不用打卡、不算缺勤），但員工要休得自己請假，故不得從請假日中扣除。
    /// 預設 false＝原語意（所有 IsHoliday = 1 的日子）—— 假日執行活動的假日天數 / 假日津貼取數
    /// （TravelRequestHandler 三處）與打卡休假日判定都吃這個預設值，彈性休假日對它們仍是假日。
    /// </param>
    Task<IReadOnlyList<DateTime>> GetHolidayDatesAsync(DateTime startDate, DateTime endDate, bool excludeFlexibleHoliday = false);
}

public sealed class CalendarDayReadService(IDbConnection db) : ICalendarDayReadService
{
    public async Task<IEnumerable<CalendarDayDto>> GetByYearAsync(int year)
    {
        const string sql = """
            SELECT Id, Date, IsHoliday, Description, Year
            FROM CalendarDays
            WHERE Year = @Year
            ORDER BY Date
            """;

        return await db.QueryAsync<CalendarDayDto>(sql, new { Year = year });
    }

    /// <summary>計算日期範圍內的放假天數</summary>
    public async Task<int> CountHolidaysAsync(DateTime startDate, DateTime endDate)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM CalendarDays
            WHERE Date >= @StartDate AND Date <= @EndDate AND IsHoliday = 1
            """;

        return await db.ExecuteScalarAsync<int>(sql, new { StartDate = startDate, EndDate = endDate });
    }

    /// <summary>取得日期範圍內的所有放假日期（供逐日假日標示與參與人員個人假日天數計算）</summary>
    public async Task<IReadOnlyList<DateTime>> GetHolidayDatesAsync(
        DateTime startDate, DateTime endDate, bool excludeFlexibleHoliday = false)
    {
        // 名稱以 Dapper 參數帶入（不寫中文字面量），單一真相為 Constants.CalendarDescriptions
        var sql = $"""
            SELECT Date
            FROM CalendarDays
            WHERE Date >= @StartDate AND Date <= @EndDate AND IsHoliday = 1
              {(excludeFlexibleHoliday ? "AND Description <> @FlexibleHoliday" : "")}
            ORDER BY Date
            """;

        var rows = await db.QueryAsync<DateTime>(sql, new
        {
            StartDate = startDate,
            EndDate   = endDate,
            FlexibleHoliday = CalendarDescriptions.FlexibleHoliday,
        });
        return rows.ToList();
    }

    /// <summary>檢查日期範圍內是否有行事曆資料（用於驗證是否已匯入）</summary>
    public async Task<bool> HasDataForRangeAsync(DateTime startDate, DateTime endDate)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM CalendarDays
                WHERE Date >= @StartDate AND Date <= @EndDate
            ) THEN 1 ELSE 0 END
            """;

        return await db.ExecuteScalarAsync<bool>(sql, new { StartDate = startDate, EndDate = endDate });
    }
}
