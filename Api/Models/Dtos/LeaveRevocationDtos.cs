namespace Jabez.Api.Models.Dtos;

/// <summary>可銷 / 已銷的單日明細</summary>
public sealed record LeaveRevocationDateDto(
    DateTime Date,
    decimal  Hours);

/// <summary>
/// 可銷假日期清單（供銷假表單逐日勾選）。
/// 已排除：已被核准銷假的日、被其他進行中銷假單佔用的日、今天以前的日。
/// </summary>
public sealed record RevocableDatesDto(
    int       LeaveRequestId,
    string    LeaveType,
    string    TimeUnit,
    DateTime  StartDate,
    DateTime  EndDate,
    decimal   Hours,
    string    Reason,
    IReadOnlyList<LeaveRevocationDateDto> Dates,
    decimal   TotalRevocableHours);

public sealed record LeaveRevocationDto(
    int       Id,
    int       LeaveRequestId,
    string    EmployeeName,
    string    Reason,
    decimal   RevokedHours,
    string    ApprovalStatus,
    DateTime  CreatedAt,
    DateTime? ReviewedAt,
    string?   ReviewNote,
    int?      ApprovalItemId   = null,
    int?      CurrentStepOrder = null,
    Guid?     ReviewedById     = null,
    IReadOnlyList<LeaveRevocationDateDto>? Dates = null,
    DesignatedReviewerDto[]? DesignatedReviewers = null,
    // 原請假單資訊（供列表 / 詳情 / 簽核頁直接顯示，免二次查詢）
    string?   LeaveType        = null,
    DateTime? LeaveStartDate   = null,
    DateTime? LeaveEndDate     = null,
    decimal?  LeaveHours       = null,
    decimal?  LeaveOriginalHours = null,
    string?   LeaveApprovalStatus = null);

public sealed record CreateLeaveRevocationRequest(
    DateTime[] Dates,
    string     Reason = "",
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

public sealed record UpdateLeaveRevocationRequest(
    DateTime[]? Dates,
    string?     Reason,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);
