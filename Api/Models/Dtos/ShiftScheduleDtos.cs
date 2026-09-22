namespace Jabez.Api.Models.Dtos;

/// <summary>
/// 排班月曆的一格。國定假日為唯讀格（<see cref="ReadOnly"/> = true 且不佔配額）。
/// </summary>
/// <param name="Date">日期</param>
/// <param name="DayType">work / rest_day / statutory_off / public_holiday</param>
/// <param name="HolidayName">國定假日名稱（如「國慶日」「補假」），非國定假日為 null</param>
/// <param name="ReadOnly">該格不可由員工勾選（目前僅國定假日，以及不在可編輯範圍內的日期）</param>
/// <param name="IsActivityDay">該日有主管排定的活動（疊加旗標，與 DayType 並存）</param>
/// <param name="ActivityTitle">活動名稱</param>
/// <param name="IsActivityAssignee">本人是否被列為該活動日的預定人力</param>
public sealed record ShiftScheduleDayDto(
    DateTime Date,
    string   DayType,
    string?  HolidayName,
    bool     ReadOnly,
    bool     IsActivityDay,
    string?  ActivityTitle,
    bool     IsActivityAssignee);

/// <summary>排班檢核結果（對應 <c>ShiftScheduleValidationResult</c>）。</summary>
public sealed record ShiftScheduleValidationDto(
    bool     CanSave,
    string[] Blocks,
    string[] Warnings,
    int      StatutoryOffCount,
    int      RestDayCount,
    int      RequiredStatutoryOff,
    int      RequiredRestDay);

/// <summary>某人某月的排班月曆。</summary>
/// <param name="Editable">此刻是否可編輯（開放期／寬限期／當日臨時調休）</param>
/// <param name="EditMode">open / grace_period / same_day_only / closed</param>
/// <param name="EditReason">可直接顯示給使用者的說明</param>
/// <param name="Status">整月狀態：draft / committed / auto；尚未建立為 null</param>
/// <param name="Validation">
/// 以目前班表算出的檢核結果。**進入畫面時就要顯示**（空白月曆必然擋存，
/// 不可等到按下儲存才報錯）。
/// </param>
public sealed record ShiftScheduleMonthDto(
    Guid     UserId,
    string   UserName,
    int      Year,
    int      Month,
    bool     Editable,
    string   EditMode,
    string   EditReason,
    string?  Status,
    DateTime? CommittedAt,
    DateTime? AutoAssignedAt,
    ShiftScheduleDayDto[]       Days,
    ShiftScheduleValidationDto  Validation);

/// <summary>整月整批替換的請求。未列出的日子一律視為上班日。</summary>
/// <param name="Days">
/// 只需送「非上班日」的格子即可（例假／休假）；送了國定假日格會被忽略。
/// </param>
public sealed record SaveShiftScheduleRequest(
    int  Year,
    int  Month,
    Guid? UserId,
    SaveShiftScheduleDayRequest[] Days);

public sealed record SaveShiftScheduleDayRequest(
    DateTime Date,
    string   DayType);
