namespace Jabez.Api.Models.Dtos;

/// <summary>改班申請的單日明細：哪一天、從什麼改成什麼。</summary>
/// <param name="FromDayType">送單當下的日別快照（簽核者看得到「原本是什麼」）</param>
/// <param name="ToDayType">要改成的日別</param>
/// <param name="IsPublicHoliday">該日為國定假日 → 不可申請變更（唯讀、不佔配額）</param>
/// ⚠ 核准後 FromDayType 仍是「送單當下」的快照；讀取時以快照為準，不可拿現行班表重算（核准後兩者相同）。
public sealed record ShiftChangeDateDto(
    DateTime Date,
    string   FromDayType,
    string   ToDayType,
    bool     IsPublicHoliday = false);

/// <summary>
/// 可申請改班的日期清單（供改班表單逐日勾選）。
/// 已排除：國定假日（唯讀）、今天以前的日期、被其他進行中改班單佔用的日期。
/// </summary>
public sealed record ChangeableShiftDatesDto(
    int      Year,
    int      Month,
    string?  MonthStatus,
    ShiftChangeDateDto[] Dates,
    /// <summary>目前的配額狀況，讓申請人知道改完會不會破壞 4 例 4 休</summary>
    int      StatutoryOffCount,
    int      RestDayCount,
    int      RequiredStatutoryOff,
    int      RequiredRestDay);

public sealed record ShiftChangeRequestDto(
    int       Id,
    string?   RequestNo,             // SC-yyyyMMdd-NNN；送簽時取號，草稿為 null
    Guid?     EmployeeId,
    string    EmployeeName,
    string?   DepartmentName,
    int       Year,
    int       Month,
    string    Reason,
    string    ApprovalStatus,
    DateTime  CreatedAt,
    DateTime? SubmittedAt,           // 送簽日期（申請日期）；草稿為 null
    DateTime? ReviewedAt,
    string?   ReviewNote,
    int?      ApprovalItemId   = null,
    int?      CurrentStepOrder = null,
    Guid?     ReviewedById     = null,
    ShiftChangeDateDto[]? Dates = null,
    DesignatedReviewerDto[]? DesignatedReviewers = null,
    ShiftChangeMonthViewDto? View = null);

public sealed record CreateShiftChangeRequest(
    int      Year,
    int      Month,
    ShiftChangeDateRequest[] Dates,
    string   Reason = "",
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

public sealed record UpdateShiftChangeRequest(
    ShiftChangeDateRequest[]? Dates,
    string?  Reason,
    DesignatedReviewerRequest[]? DesignatedReviewers = null,
    int?     Year  = null,       // 草稿換月份（null＝不變）
    int?     Month = null);

/// <summary>
/// 改班「套用後」的整月檢視（表單即時試算、申請詳情、簽核頁三處共用同一份）。
/// </summary>
/// <param name="Days">套用本次調整後的每一天（DayType＝調整後日別；ReadOnly＝這一格不可申請變更）</param>
/// <param name="Changes">本次調整（原 → 新）</param>
/// <param name="Validation">套用後的整月檢核（三條排班規則 ＋ 活動日／請假鎖定），與個人排班同一真相</param>
public sealed record ShiftChangeMonthViewDto(
    int      Year,
    int      Month,
    ShiftScheduleDayDto[] Days,
    ShiftChangeDateDto[]  Changes,
    ShiftScheduleValidationDto Validation);

/// <summary>POST /shift-changes/preview：只試算、不寫入。</summary>
/// <param name="ExcludeRequestId">編輯既有單時帶自己的 Id，避免自己佔用的日期被判成「已被其他改班單佔用」</param>
public sealed record PreviewShiftChangeRequest(
    int      Year,
    int      Month,
    ShiftChangeDateRequest[]? Dates,
    int?     ExcludeRequestId = null);

public sealed record ShiftChangeDateRequest(
    DateTime Date,
    string   ToDayType);
