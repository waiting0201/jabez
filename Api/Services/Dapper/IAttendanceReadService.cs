using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;

namespace Jabez.Api.Services.Dapper;

public interface IAttendanceReadService
{
    /// <summary>
    /// 區間內的全部打卡列（不分頁、不含請假欄位）。
    /// 出缺勤報表需與請假日合併後才能切頁，故分頁由 AttendanceLeaveMerger 在記憶體端負責。
    /// </summary>
    Task<IReadOnlyList<AttendanceRecordDto>> ListInRangeAsync(
        ProjectAccessScope scope, Guid? employeeId, DateOnly dateFrom, DateOnly dateTo);

    /// <summary>區間內與該區間有交集的已核准請假單（尚未逐日展開、尚未排除銷假日）。</summary>
    Task<IReadOnlyList<AttendanceLeaveSourceRow>> ListApprovedLeavesInRangeAsync(
        ProjectAccessScope scope, Guid? employeeId, DateOnly dateFrom, DateOnly dateTo);

    /// <summary>指定假單清單的已核准銷假日（批次）。清單為空時回空集合，不送 SQL。</summary>
    Task<IReadOnlyList<LeaveRevokedDateRow>> ListApprovedRevokedDatesAsync(
        IReadOnlyCollection<int> leaveRequestIds);

    Task<TodayAttendanceDto?>              GetTodayAsync(Guid userId);

    /// <summary>取得指定時刻落在 [StartDate, EndDate) 區間內的最早一筆已核准請假；無則回 null。</summary>
    Task<ActiveLeaveDto?>                  GetActiveLeaveAtAsync(Guid userId, DateTime when);

    /// <summary>取得指定日期內所有與該日有時段交集的已核准請假（含尚未開始 / 已結束的時段，供前端提示）。</summary>
    Task<IReadOnlyList<ActiveLeaveDto>>    GetLeavesOnDateAsync(Guid userId, DateOnly date);
}
