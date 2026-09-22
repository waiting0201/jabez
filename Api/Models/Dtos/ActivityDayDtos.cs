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
/// 改期後「排班被影響、且重跑檢核不通過」的同仁。
///
/// 規格 §3.2：活動日得因業主通知或天氣因素改期，改期後系統對受影響同仁重跑三條檢核，
/// 不通過者告知其自行送〈改班申請〉調整 —— **系統不自動改寫個人已定案的班表**
/// （班表是同仁自己排的，自動改寫會讓人在不知情下被調班）。
/// </summary>
public sealed record AffectedScheduleDto(
    Guid     UserId,
    string   UserName,
    int      Year,
    int      Month,
    string[] Blocks);

/// <summary>新增／改期活動日的回應。<see cref="Affected"/> 非空時前端要提示主管通知這些同仁。</summary>
public sealed record SaveActivityDayResultDto(
    ActivityDayDto        ActivityDay,
    bool                  DateChanged,
    AffectedScheduleDto[] Affected);
