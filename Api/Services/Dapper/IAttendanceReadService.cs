using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;

namespace Jabez.Api.Services.Dapper;

public interface IAttendanceReadService
{
    Task<PagedResult<AttendanceRecordDto>> GetPagedAsync(ProjectAccessScope scope, int page, int pageSize,
        Guid? employeeId = null, DateOnly? dateFrom = null, DateOnly? dateTo = null);
    Task<TodayAttendanceDto?>              GetTodayAsync(Guid userId);

    /// <summary>取得指定時刻落在 [StartDate, EndDate) 區間內的最早一筆已核准請假；無則回 null。</summary>
    Task<ActiveLeaveDto?>                  GetActiveLeaveAtAsync(Guid userId, DateTime when);

    /// <summary>取得指定日期內所有與該日有時段交集的已核准請假（含尚未開始 / 已結束的時段，供前端提示）。</summary>
    Task<IReadOnlyList<ActiveLeaveDto>>    GetLeavesOnDateAsync(Guid userId, DateOnly date);
}
