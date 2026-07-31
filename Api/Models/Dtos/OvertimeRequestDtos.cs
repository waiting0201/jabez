namespace Jabez.Api.Models.Dtos;

/// <summary>加班申請的關聯專案明細（讀取用；加班報表與簽核任務詳情共用）</summary>
public sealed record OvertimeProjectDto(
    int     ProjectId,
    string  ProjectCode,
    string  ProjectName,
    decimal EstimatedHours);

/// <summary>加班申請的關聯專案明細（寫入用；Create / Update 共用）</summary>
public sealed record OvertimeProjectRequest(
    int     ProjectId,
    decimal EstimatedHours);

public sealed record OvertimeRequestDto(
    int       Id,
    string    EmployeeName,
    DateTime  OvertimeDate,
    OvertimeProjectDto[] Projects,
    decimal   EstimatedHours,          // 合計 = Projects 各列加總
    string    Reason,
    string    ApprovalStatus,
    DateTime  CreatedAt,
    DateTime? ReviewedAt,
    string?   ReviewNote,
    int?      ApprovalItemId       = null,
    int?      CurrentStepOrder     = null,
    Guid?     ReviewedById         = null,
    DesignatedReviewerDto[]? DesignatedReviewers = null);

public sealed record CreateOvertimeRequestRequest(
    Guid?    EmployeeId,
    int?     ApprovalItemId       = null,
    DateTime OvertimeDate         = default,
    OvertimeProjectRequest[]? Projects = null,   // 必填，至少 1 列（Handler 驗證）
    string   Reason               = "",
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

public sealed record UpdateOvertimeRequestRequest(
    DateTime? OvertimeDate,
    OvertimeProjectRequest[]? Projects,          // 必填，至少 1 列（整批替換，不支援省略）
    string?   Reason,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);
