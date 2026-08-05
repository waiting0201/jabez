namespace Jabez.Api.Models.Dtos;

public sealed record AttendanceRecordDto(
    int       Id,
    string    UserName,
    DateTime  RecordDate,
    DateTime? ClockInTime,
    double?   ClockInLatitude,
    double?   ClockInLongitude,
    DateTime? ClockOutTime,
    double?   ClockOutLatitude,
    double?   ClockOutLongitude,
    /// <summary>下班時間為登入時系統自動補卡（非本人打卡），出缺勤清單以 badge 標示</summary>
    bool      IsClockOutAuto,
    DateTime? OvertimeStartTime,
    double?   OvertimeStartLatitude,
    double?   OvertimeStartLongitude,
    DateTime? OvertimeEndTime,
    double?   OvertimeEndLatitude,
    double?   OvertimeEndLongitude,
    int?      OvertimeRequestId,
    DateTime  CreatedAt,
    string?   LeaveType,
    DateTime? LeaveStartDate,
    DateTime? LeaveEndDate);

public sealed record TodayAttendanceDto(
    int       Id,
    DateTime  RecordDate,
    DateTime? ClockInTime,
    double?   ClockInLatitude,
    double?   ClockInLongitude,
    DateTime? ClockOutTime,
    double?   ClockOutLatitude,
    double?   ClockOutLongitude,
    DateTime? OvertimeStartTime,
    double?   OvertimeStartLatitude,
    double?   OvertimeStartLongitude,
    DateTime? OvertimeEndTime,
    double?   OvertimeEndLatitude,
    double?   OvertimeEndLongitude,
    int?      OvertimeRequestId,
    IReadOnlyList<ActiveLeaveDto> TodayLeaves,
    /// <summary>
    /// 今日免下班卡即可打「加班開始」（休假日或全日請假）。
    /// 與 AttendanceHandler.OvertimeStartAsync 的放行判定同源，前端不自行重組規則。
    /// 有預設值 → Dapper 的 MapTodayRow 不需異動（此欄位不存在於 DB，由 Handler 以 with { } 補上）。
    /// </summary>
    bool CanOvertimeWithoutClockOut = false);

/// <summary>當下落在已核准請假時段內的請假資訊（供打卡阻擋訊息與前端提示）</summary>
public sealed record ActiveLeaveDto(
    int      Id,
    string   LeaveType,
    DateTime StartDate,
    DateTime EndDate);

public sealed record ClockActionRequest(
    double? Latitude,
    double? Longitude,
    int?    OvertimeRequestId = null);

/// <summary>修改出缺勤紀錄（僅允許調整四個時間欄位）</summary>
public sealed record UpdateAttendanceRequest(
    DateTime? ClockInTime,
    DateTime? ClockOutTime,
    DateTime? OvertimeStartTime,
    DateTime? OvertimeEndTime);
