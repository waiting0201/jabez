namespace Jabez.Api.Models.Dtos;

/// <summary>
/// 出缺勤報表列。列表為「打卡紀錄 ∪ 當日請假日」的合併結果（見 AttendanceLeaveMerger）：
/// <c>Id = null</c> 代表「當日只有已核准請假、沒有任何打卡紀錄」的請假虛擬列（前端據此不顯示編輯鈕）。
/// </summary>
public sealed record AttendanceRecordDto(
    /// <summary>AttendanceRecord.Id；null＝請假虛擬列（DB 無對應紀錄，不可編輯）</summary>
    int?      Id,
    /// <summary>員工 Id。虛擬列沒有 Id，前端的 track key 與分組一律靠此欄</summary>
    Guid      UserId,
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
    /// <summary>AttendanceRecord.CreatedAt；虛擬列為 null</summary>
    DateTime? CreatedAt,
    /// <summary>當日第一張假單的假別（相容欄位，多張假單的完整清單見 <see cref="Leaves"/>）</summary>
    string?   LeaveType,
    /// <summary>當日第一張假單的起日（整張單的區間，非當日）</summary>
    DateTime? LeaveStartDate,
    /// <summary>當日第一張假單的訖日（整張單的區間，非當日）</summary>
    DateTime? LeaveEndDate,
    /// <summary>當日請假時數合計（同日多張假單加總）。無請假為 null</summary>
    decimal?  LeaveHours = null,
    /// <summary>當日所有已核准（且該日未被銷假）的請假，依 StartDate 排序。無請假為 null</summary>
    IReadOnlyList<AttendanceLeaveDto>? Leaves = null);

/// <summary>出缺勤報表列中的單張請假資訊（當日份）</summary>
public sealed record AttendanceLeaveDto(
    int      LeaveRequestId,
    string   LeaveType,
    /// <summary>該假單「當日」的時數（由 LeaveDayExpander 逐日展開，非整張單的 Hours）</summary>
    decimal  Hours,
    DateTime StartDate,
    DateTime EndDate);

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

/// <summary>
/// 出缺勤報表合併用的原料列：區間內已核准的假單（尚未逐日展開）。
/// 僅供 AttendanceLeaveMerger 內部使用，不直接回傳給前端。
/// </summary>
public sealed record AttendanceLeaveSourceRow(
    int      Id,
    Guid     UserId,
    string   UserName,
    string   LeaveType,
    DateTime StartDate,
    DateTime EndDate);

/// <summary>
/// 出缺勤報表合併用的原料列：已核准銷假的逐日紀錄（批次查詢結果）。
/// </summary>
public sealed record LeaveRevokedDateRow(
    int      LeaveRequestId,
    DateTime Date);

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
