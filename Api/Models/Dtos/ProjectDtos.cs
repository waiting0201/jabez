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
    decimal? ReceivedAmount,
    decimal? ContractAmount,
    decimal? BusinessAmount,
    string?  GoogleDriveUrl,
    DateTime CreatedAt,
    IReadOnlyList<ProjectPaymentScheduleDto> PaymentSchedules);

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
    DateTime? EndDate        = null,
    string?  Status         = null,
    decimal? ReceivedAmount = null,
    decimal? ContractAmount = null,
    decimal? BusinessAmount = null,
    string?  GoogleDriveUrl = null,
    IReadOnlyList<ProjectPaymentScheduleRequest>? PaymentSchedules = null);

public sealed record UpdateProjectRequest(
    string?   Code,
    string?   Name,
    string?   Status,
    DateTime? StartDate,
    DateTime? EndDate,
    int?      DepartmentId,
    decimal?  ReceivedAmount,
    decimal?  ContractAmount,
    decimal?  BusinessAmount,
    string?   GoogleDriveUrl,
    IReadOnlyList<ProjectPaymentScheduleRequest>? PaymentSchedules);
