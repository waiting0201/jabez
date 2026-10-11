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

    /// <summary>
    /// 區間內「應出勤」的員工母體（供出缺勤報表的缺勤虛擬列）。
    /// 條件＝非超管 + 在職 + 持有 attendances:write，並套用部門可見性 scope。
    /// </summary>
    Task<IReadOnlyList<AttendanceEmployeeRow>> ListClockingEmployeesAsync(
        ProjectAccessScope scope, Guid? employeeId, DateOnly dateFrom, DateOnly dateTo);

    Task<TodayAttendanceDto?>              GetTodayAsync(Guid userId);

    /// <summary>
    /// 指定日期「加班已開始、尚未結束」的打卡紀錄；無則回 null。
    /// 供跨日加班（過午夜）在隔天凌晨補打結束卡：紀錄的 RecordDate 是加班開始那天，
    /// 打卡頁以今天撈不到，須另外撈前一天。
    /// </summary>
    Task<TodayAttendanceDto?>              GetOpenOvertimeOnAsync(Guid userId, DateTime day);

    /// <summary>取得指定時刻落在 [StartDate, EndDate) 區間內的最早一筆已核准請假；無則回 null。</summary>
    Task<ActiveLeaveDto?>                  GetActiveLeaveAtAsync(Guid userId, DateTime when);

    /// <summary>取得指定日期內所有與該日有時段交集的已核准請假（含尚未開始 / 已結束的時段，供前端提示）。</summary>
    Task<IReadOnlyList<ActiveLeaveDto>>    GetLeavesOnDateAsync(Guid userId, DateOnly date);
}
