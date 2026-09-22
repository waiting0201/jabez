using System.Data;
using Dapper;
using Jabez.Api.Common;

namespace Jabez.Api.Services.Dapper;

/// <summary>
/// 「某人某日的日別」解析 —— 四週彈性工時取代 <c>WorkCalendarHelper</c> 的 <c>bool ignoreHolidays</c> 的入口。
///
/// 解析優先序（三段，缺一不可）：
///   <list type="number">
///     <item>該員該日有 <c>ShiftScheduleDay</c> → 用它（上班日／例假日／休假日）</item>
///     <item>否則該日為**國定假日**（<c>CalendarDay.IsHoliday</c> 且 <c>Description</c> 非空）→ public_holiday</item>
///     <item>否則 → 依舊制行事曆判定（有資料看 IsHoliday、無資料退回六日），回 work / rest_day</item>
///   </list>
///
/// 第 3 段是**切換日之前的歷史日期**與「尚未排班的未來日期」的退路，
/// 讓本服務在制度切換前後都能回答，呼叫端不必自己判斷切換與否。
///
/// ⚠ **國定假日判準必須用 GetByYearAsync（有 Description），不可用 GetHolidayDatesAsync**：
/// 後者只回日期，分不出「國慶日」與「單純的週六」—— 公司行事曆把週六日也標成 IsHoliday = 1。
/// 判準收斂在 <see cref="PublicHolidayRule"/>，本服務與月曆讀取、整月寫入、配額重算共用同一份。
///
/// ⚠ **效能**：per-user 解析後，出缺勤報表原本「依 IsShiftWorker 分兩組、整趟最多 2 次工作日計算」的
/// memo 失效。故區間查詢一律走 <see cref="ResolveRangeForUsersAsync"/> 一次撈回全部人的排班（單次 SQL），
/// 在記憶體內 per-user 分組，不要逐人呼叫單日版。
/// </summary>
public interface IShiftScheduleReadService
{
    /// <summary>單日版：該員該日的日別（<c>WorkDayTypes</c> 四值之一）。</summary>
    Task<string> ResolveDayTypeAsync(Guid userId, DateTime date);

    /// <summary>區間版：該員 [from, to] 每一天的日別。</summary>
    Task<IReadOnlyDictionary<DateTime, string>> ResolveRangeAsync(Guid userId, DateTime from, DateTime to);

    /// <summary>
    /// 批次版：一次解析多人的整個區間，供出缺勤報表 / 排休總覽表使用。
    /// <paramref name="userIds"/> 為空 ＝ 不限制（全公司）。
    /// </summary>
    Task<IReadOnlyDictionary<(Guid UserId, DateTime Date), string>> ResolveRangeForUsersAsync(
        IReadOnlyCollection<Guid> userIds, DateTime from, DateTime to);

    /// <summary>該員該日是否被列為某個活動日的預定人力（活動日是疊加旗標，非日別）。</summary>
    Task<bool> IsActivityAssigneeAsync(Guid userId, DateTime date);
}

public sealed class ShiftScheduleReadService(IDbConnection db, ICalendarDayReadService calendarReader)
    : IShiftScheduleReadService
{
    // 以年為粒度快取「國定假日集合」，避免逐日打 DB。生命週期＝單次請求（Scoped）。
    private readonly Dictionary<int, HashSet<DateTime>> _publicHolidaysByYear = [];
    // 該年度行事曆是否有資料（無資料時第 3 段退回六日判定）。
    private readonly Dictionary<int, bool> _hasCalendarByYear = [];

    public async Task<string> ResolveDayTypeAsync(Guid userId, DateTime date)
    {
        var map = await ResolveRangeAsync(userId, date, date);
        return map.TryGetValue(date.Date, out var t) ? t : WorkDayTypes.Work;
    }

    public async Task<IReadOnlyDictionary<DateTime, string>> ResolveRangeAsync(Guid userId, DateTime from, DateTime to)
    {
        var all = await ResolveRangeForUsersAsync([userId], from, to);
        return all.Where(kv => kv.Key.UserId == userId)
                  .ToDictionary(kv => kv.Key.Date, kv => kv.Value);
    }

    public async Task<IReadOnlyDictionary<(Guid UserId, DateTime Date), string>> ResolveRangeForUsersAsync(
        IReadOnlyCollection<Guid> userIds, DateTime from, DateTime to)
    {
        var start = from.Date;
        var end   = to.Date;

        // ① 個人排班：一次撈回整段（單次 SQL，不逐人查）
        var sql = """
            SELECT UserId, Date, DayType
            FROM ShiftScheduleDays
            WHERE Date >= @Start AND Date <= @End
        """;
        if (userIds.Count > 0) sql += " AND UserId IN @UserIds";

        var rows = await db.QueryAsync<(Guid UserId, DateTime Date, string DayType)>(
            sql, new { Start = start, End = end, UserIds = userIds });

        var scheduled = rows.ToDictionary(r => (r.UserId, r.Date.Date), r => WorkDayTypes.Normalize(r.DayType));

        // ② / ③ 沒排到的日子：國定假日 → 舊制行事曆判定
        await EnsureCalendarLoadedAsync(start, end);

        var result = new Dictionary<(Guid, DateTime), string>(scheduled.Count);
        foreach (var kv in scheduled) result[kv.Key] = kv.Value;

        // userIds 為空（全公司）時無法枚舉人員，只回已排班的部分；
        // 呼叫端若需要「每人每天都有值」請傳明確的 userIds。
        if (userIds.Count == 0) return result;

        foreach (var uid in userIds)
        {
            for (var d = start; d <= end; d = d.AddDays(1))
            {
                if (result.ContainsKey((uid, d))) continue;
                result[(uid, d)] = ResolveUnscheduled(d);
            }
        }
        return result;
    }

    public async Task<bool> IsActivityAssigneeAsync(Guid userId, DateTime date)
    {
        const string sql = """
            SELECT TOP 1 1
            FROM ActivityDayAssignees a
            JOIN ActivityDays d ON d.Id = a.ActivityDayId
            WHERE a.UserId = @UserId AND d.Date = @Date
        """;
        return await db.ExecuteScalarAsync<int?>(sql, new { UserId = userId, Date = date.Date }) is not null;
    }

    /// <summary>沒有個人排班時的退路：國定假日 → 舊制行事曆（無資料退回六日）。</summary>
    private string ResolveUnscheduled(DateTime date)
    {
        var year = date.Year;

        if (_publicHolidaysByYear.TryGetValue(year, out var holidays) && holidays.Contains(date))
            return WorkDayTypes.PublicHoliday;

        // 該年度沒有行事曆資料 → 退回六日判定（與 WorkCalendarHelper 同一份規則）
        if (!_hasCalendarByYear.TryGetValue(year, out var hasData) || !hasData)
            return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                ? WorkDayTypes.RestDay
                : WorkDayTypes.Work;

        // 有行事曆但不是國定假日：週末算休假日、其餘為上班日
        // （補班六在行事曆中 IsHoliday = 0，會落到 Work，與現行行為一致）
        return _weekendByYear[year].Contains(date) ? WorkDayTypes.RestDay : WorkDayTypes.Work;
    }

    private readonly Dictionary<int, HashSet<DateTime>> _weekendByYear = [];

    private async Task EnsureCalendarLoadedAsync(DateTime start, DateTime end)
    {
        for (var y = start.Year; y <= end.Year; y++)
        {
            if (_publicHolidaysByYear.ContainsKey(y)) continue;

            var days = (await calendarReader.GetByYearAsync(y)).ToList();
            _hasCalendarByYear[y] = days.Count > 0;

            // 國定假日：IsHoliday 且 Description 非空（週六日的 Description 為空，會被排除）
            _publicHolidaysByYear[y] = [.. days
                .Where(d => PublicHolidayRule.IsPublicHoliday(d.IsHoliday, d.Description))
                .Select(d => d.Date.Date)];

            // 行事曆標為假日、但沒有名稱者 ＝ 單純的週末（含調整後的補假安排）
            _weekendByYear[y] = [.. days
                .Where(d => d.IsHoliday && !PublicHolidayRule.IsPublicHoliday(d.IsHoliday, d.Description))
                .Select(d => d.Date.Date)];
        }
    }
}
