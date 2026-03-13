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
    int?      OvertimeRequestId);

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
