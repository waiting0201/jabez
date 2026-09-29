namespace Jabez.Api.Models.Dtos;

/// <summary>活動日的預定人力。</summary>
public sealed record ActivityDayAssigneeDto(
    Guid    UserId,
    string  UserName,
    string? DepartmentName,
    string? JobTitleName);

/// <summary>
/// 主管排定的活動日。⚠ 這是**疊加在日別之上的旗標**，不是第 5 種日別 ——
/// 同一天可以既是「上班日」又是「活動日」，也可以壓在「國定假日」上。
/// </summary>
public sealed record ActivityDayDto(
    int      Id,
    DateTime Date,
    int      DepartmentId,
    string?  DepartmentName,
    string   Title,
    Guid     CreatedByUserId,
    string?  CreatedByName,
    bool     IsPublicHoliday,
    string?  HolidayName,
    ActivityDayAssigneeDto[] Assignees);

public sealed record SaveActivityDayRequest(
    DateTime Date,
    int      DepartmentId,
    string   Title,
    Guid[]   AssigneeUserIds);

/// <summary>
/// 存檔後「班表重跑檢核仍不通過」的同仁（班表本身未排完或不合規）。
///
/// 活動日當天排了例假／休假的衝突**不在此列** —— 2026-09-29 起改為活動日優先、由系統自動覆蓋並補排，
/// 見 <see cref="ActivityScheduleAdjustedDto"/>。
/// </summary>
public sealed record AffectedScheduleDto(
    Guid     UserId,
    string   UserName,
    int      Year,
    int      Month,
    string[] Blocks);

/// <summary>
/// 被活動日覆蓋的一筆排班（2026-09-29 活動日優先）：該員當天原排例假／休假，已改為上班日。
/// </summary>
/// <param name="OriginalDayType">statutory_off / rest_day</param>
/// <param name="RelocatedTo">原例假／休假搬到的日子；null ＝ 找不到合法的日子，待同仁自行補排。</param>
public sealed record ActivityScheduleAdjustedDto(
    Guid      UserId,
    string    UserName,
    DateTime  Date,
    string    OriginalDayType,
    DateTime? RelocatedTo);

/// <summary>
/// 新增／改期活動日的回應。<see cref="Adjusted"/> 為系統已自動覆蓋的班表（同仁會收到鈴鐺通知），
/// <see cref="Affected"/> 為班表仍不合規、需要主管留意的同仁。
/// </summary>
public sealed record SaveActivityDayResultDto(
    ActivityDayDto                ActivityDay,
    bool                          DateChanged,
    ActivityScheduleAdjustedDto[] Adjusted,
    AffectedScheduleDto[]         Affected);
