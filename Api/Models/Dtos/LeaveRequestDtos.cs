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
    string?   TimeUnit             = null);

public sealed record CreateLeaveRequestRequest(
    Guid?    EmployeeId,
    int?     ApprovalItemId       = null,
    string   LeaveType            = "annual",
    DateTime StartDate            = default,
    DateTime EndDate              = default,
    decimal  Hours                = 1m,
    string   Reason               = "",
    string?  BereavementRelationship = null,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

public sealed record UpdateLeaveRequestRequest(
    string?   LeaveType,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal?  Hours,
    string?   Reason,
    string?   BereavementRelationship = null,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

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

/// <summary>重疊請假申請（內部用，組合衝突錯誤訊息）</summary>
public sealed record OverlappingLeaveRequestDto(
    int      Id,
    string   LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    string   ApprovalStatus,
    decimal  Hours);
