namespace Jabez.Api.Models.Dtos;

/// <summary>總覽表中某人某一天的格子。</summary>
/// <param name="Day">當月第幾日（1-31）</param>
/// <param name="DayType">work / rest_day / statutory_off / public_holiday</param>
/// <param name="IsActivityDay">該日有活動（疊加旗標，與 DayType 並存 —— 總覽表的第三種顏色）</param>
/// <param name="IsActivityAssignee">本人是否為該活動日的預定人力</param>
public sealed record ShiftOverviewCellDto(
    int    Day,
    string DayType,
    bool   IsActivityDay,
    bool   IsActivityAssignee);

/// <summary>總覽表的一列（一位同仁）。</summary>
/// <param name="QuotaSatisfied">
/// 配額是否已滿（例假 ∧ 休假皆達標）。未達標者前端顯示「尚未選滿」，
/// 這是主管在開放期內催排班的依據。
/// </param>
public sealed record ShiftOverviewRowDto(
    Guid    UserId,
    string  UserName,
    string? DepartmentName,
    int     StatutoryOffCount,
    int     RestDayCount,
    int     RequiredStatutoryOff,
    int     RequiredRestDay,
    bool    QuotaSatisfied,
    ShiftOverviewCellDto[] Cells);

/// <summary>某一天的出勤人數統計（表尾列）。</summary>
/// <param name="Weekday">0 = 週日 … 6 = 週六</param>
/// <param name="WorkingCount">當日排定上班的人數（國定假日與例假／休假不計）</param>
/// <param name="NoCoverage">
/// **週一至週五**卻無任何人出勤 —— 需警示。
/// 週末與國定假日本來就可能全員休假，不算異常。
/// </param>
public sealed record ShiftOverviewDayStatDto(
    int      Day,
    DateTime Date,
    int      Weekday,
    bool     IsPublicHoliday,
    string?  HolidayName,
    int      WorkingCount,
    bool     NoCoverage);

/// <summary>〈出勤／排休總覽表〉。</summary>
public sealed record ShiftOverviewDto(
    int    Year,
    int    Month,
    int    DaysInMonth,
    int?   DepartmentId,
    ShiftOverviewDayStatDto[] DayStats,
    ShiftOverviewRowDto[]     Rows);
