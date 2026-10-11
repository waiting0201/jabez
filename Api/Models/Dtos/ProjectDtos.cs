namespace Jabez.Api.Models.Dtos;

public sealed record ProjectDto(
    int      Id,
    string   Code,
    string   Name,
    string   Status,
    DateTime StartDate,
    DateTime? EndDate,
    int      DepartmentId,
    string?  DepartmentName,
    decimal? ReceivedAmount,        // 衍生計算值：SUM(ProjectPaymentSchedules.DepositAmount)
    decimal? ContractAmount,
    decimal? BusinessAmount,
    decimal? RemainingAmount,       // 剩餘金額（系統導入時的契約剩餘預算，非必填）
    string?  GoogleDriveUrl,
    DateTime CreatedAt,
    IReadOnlyList<ProjectPaymentScheduleDto> PaymentSchedules);

/// <summary>
/// GET /projects/active 的輕量回應（下拉用）。刻意不含 ContractAmount / BusinessAmount / ReceivedAmount /
/// RemainingAmount / GoogleDriveUrl —— 該端點免 projects:read，?all=true 更不過濾部門。
/// </summary>
public sealed record ProjectLookupDto(
    int     Id,
    string  Code,
    string  Name,
    string  Status,
    int     DepartmentId,
    string? DepartmentName);

public sealed record ProjectPaymentScheduleDto(
    Guid      Id,
    int       PeriodNo,
    DateTime? BillingDate,
    decimal?  BillingAmount,
    DateTime? InvoiceDate,
    decimal?  InvoiceAmount,
    DateTime? DepositDate,
    decimal?  DepositAmount,
    string?   DeductionNote);

public sealed record ProjectPaymentScheduleRequest(
    Guid?     Id,
    int       PeriodNo,
    DateTime? BillingDate   = null,
    decimal?  BillingAmount = null,
    DateTime? InvoiceDate   = null,
    decimal?  InvoiceAmount = null,
    DateTime? DepositDate   = null,
    decimal?  DepositAmount = null,
    string?   DeductionNote = null);

public sealed record CreateProjectRequest(
    string   Code,
    string   Name,
    DateTime StartDate,
    int      DepartmentId,
    DateTime? EndDate         = null,
    string?  Status           = null,
    decimal? ContractAmount   = null,
    decimal? BusinessAmount   = null,
    decimal? RemainingAmount  = null,
    string?  GoogleDriveUrl   = null,
    IReadOnlyList<ProjectPaymentScheduleRequest>? PaymentSchedules = null);

public sealed record UpdateProjectRequest(
    string?   Code,
    string?   Name,
    string?   Status,
    DateTime? StartDate,
    DateTime? EndDate,
    int?      DepartmentId,
    decimal?  ContractAmount,
    decimal?  BusinessAmount,
    decimal?  RemainingAmount,
    string?   GoogleDriveUrl,
    IReadOnlyList<ProjectPaymentScheduleRequest>? PaymentSchedules);
