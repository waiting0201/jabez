namespace Jabez.Api.Models.Dtos;

public sealed record PaymentReportDto(
    int       Id,
    string    EmployeeName,
    string    Type,
    string    ProjectCode,
    string    ProjectName,
    string[]  InvoiceNos,
    decimal   TotalAmount,
    string    ApprovalStatus,
    DateTime? PaidAt,
    DateTime  CreatedAt);
