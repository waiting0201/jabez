using System.Data;
using Dapper;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface ICalendarDayReadService
{
    Task<IEnumerable<CalendarDayDto>> GetByYearAsync(int year);
    Task<int> CountHolidaysAsync(DateTime startDate, DateTime endDate);
    Task<bool> HasDataForRangeAsync(DateTime startDate, DateTime endDate);
    Task<IReadOnlyList<DateTime>> GetHolidayDatesAsync(DateTime startDate, DateTime endDate);
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
    public async Task<IReadOnlyList<DateTime>> GetHolidayDatesAsync(DateTime startDate, DateTime endDate)
    {
        const string sql = """
            SELECT Date
            FROM CalendarDays
            WHERE Date >= @StartDate AND Date <= @EndDate AND IsHoliday = 1
            ORDER BY Date
            """;

        var rows = await db.QueryAsync<DateTime>(sql, new { StartDate = startDate, EndDate = endDate });
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
