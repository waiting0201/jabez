using Jabez.Api.Common;
using Jabez.Api.Services.Dapper;

namespace Jabez.Api.Services;

/// <summary>
/// 建立某位員工的 <see cref="EmployeeWorkdays"/>（組好切換日、排班制旗標與個人排班解析）。
/// 一律以「假單所有人 / 打卡本人」解析，不可用呼叫者 id（銷假、簽核常由主管或代理人操作）。
/// </summary>
public interface IEmployeeWorkdaysFactory
{
    Task<EmployeeWorkdays> ForAsync(Guid userId);
}

public sealed class EmployeeWorkdaysFactory(
    ICalendarDayReadService calendarReader,
    IWorkPatternReadService workPattern,
    IWorkdayScheduleProvider scheduleProvider,
    IShiftScheduleReadService shiftReader) : IEmployeeWorkdaysFactory
{
    public async Task<EmployeeWorkdays> ForAsync(Guid userId) => new(
        calendarReader,
        await workPattern.IsShiftWorkerAsync(userId),
        await scheduleProvider.GetSwitchDateAsync(),
        (from, to) => shiftReader.ResolveRangeAsync(userId, from, to));
}
