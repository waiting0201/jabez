namespace Jabez.Api.Models.Dtos;

public sealed record LeaveRequestDto(
    int       Id,
    string    EmployeeName,
    string    LeaveType,
    DateTime  StartDate,
    DateTime  EndDate,
    decimal   Hours,
    string    Reason,
    string    ApprovalStatus,
    DateTime  CreatedAt,
    DateTime? ReviewedAt,
    string?   ReviewNote,
    int?      ApprovalItemId       = null,
    int?      CurrentStepOrder     = null,
    Guid?     ReviewedById         = null,
    string?   BereavementRelationship = null,
    DesignatedReviewerDto[]? DesignatedReviewers = null,
    string?   TimeUnit             = null,
    Guid?     AgentUserId          = null,
    string?   AgentName            = null,
    decimal?  OriginalHours        = null,   // 有值代表曾銷假；原始請假時數
    DateTime? ChildBirthDate       = null,   // 育嬰留停：子女出生日期
    bool?     ContinueInsurance    = null);  // 育嬰留停：期間是否續保勞健保（僅記錄意願）

public sealed record CreateLeaveRequestRequest(
    Guid?    EmployeeId,
    int?     ApprovalItemId       = null,
    string   LeaveType            = "annual",
    DateTime StartDate            = default,
    DateTime EndDate              = default,
    decimal  Hours                = 1m,
    string   Reason               = "",
    string?  BereavementRelationship = null,
    Guid?    AgentUserId          = null,
    DateTime? ChildBirthDate      = null,
    bool?    ContinueInsurance    = null,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

public sealed record UpdateLeaveRequestRequest(
    string?   LeaveType,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal?  Hours,
    string?   Reason,
    string?   BereavementRelationship = null,
    Guid?     AgentUserId          = null,
    DateTime? ChildBirthDate      = null,
    bool?     ContinueInsurance   = null,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

/// <summary>
/// 育嬰留職停薪配額回應。
/// 兩層額度：每名子女合計 730 天（2 年，兩種育嬰假別併計）＋ 彈性單日每人每年 30 日。
/// 「雙親合計 60 日」無法驗證（配偶可能不在同一公司），僅於前端提示。
/// </summary>
public sealed record ParentalQuotaDto(
    bool      IsEligible,          // 在職年資是否符合（滿 6 個月）
    int       SeniorityMonths,
    bool      ChildAgeValid,       // 子女是否未滿 3 歲
    DateTime? ChildBirthDate,
    int       TotalDays,           // 730
    decimal   UsedDays,            // 該名子女已使用（兩種育嬰假別合計）
    decimal   AvailableDays,
    int       DailyYearLimit,      // 30
    decimal   DailyYearUsed,       // 當年度彈性單日已使用
    decimal   DailyYearAvailable,
    string?   Message = null);

/// <summary>婚假配額回應</summary>
public sealed record MarriageQuotaDto(
    int     MaxDays,
    decimal UsedDays,
    decimal RemainingDays);

/// <summary>產假狀態回應（一次請完制，檢查是否已有活躍申請）</summary>
public sealed record MaternityStatusDto(
    bool      HasActiveRequest,
    int?      ActiveRequestId,
    DateTime? StartDate,
    DateTime? EndDate,
    string?   ApprovalStatus);

/// <summary>喪假配額回應（依親屬關係）</summary>
public sealed record BereavementQuotaDto(
    string  Relationship,
    int     MaxDays,
    decimal UsedDays,
    decimal RemainingDays);

/// <summary>高階主管假適用性回應</summary>
public sealed record SeniorExecutiveEligibilityDto(
    bool IsEligible,
    int? JobTitleLevel);

/// <summary>請假日 / 假日計算回應（扣除國定假日與六日後的實際請假日清單）</summary>
public sealed record WorkingDaysDto(
    bool                    HasCalendarData,
    IReadOnlyList<DateTime> HolidayDates,
    IReadOnlyList<DateTime> WorkingDates,
    int                     WorkingDays);

/// <summary>重疊請假申請（內部用，組合衝突錯誤訊息）</summary>
public sealed record OverlappingLeaveRequestDto(
    int      Id,
    string   LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    string   ApprovalStatus,
    decimal  Hours);
