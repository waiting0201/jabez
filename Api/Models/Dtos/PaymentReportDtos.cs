namespace Jabez.Api.Models.Dtos;

public sealed record PaymentReportDto(
    int       Id,
    string    RequestNo,
    string    EmployeeName,
    string    Type,
    string    ProjectCode,
    string    ProjectName,
    string[]  InvoiceNos,
    decimal   TotalAmount,
    string    ApprovalStatus,
    DateTime? PaidAt,
    DateTime  CreatedAt);

/// <summary>
/// 款項統計匯出列：一張發票一列。
/// 一張請款單若有 N 張發票 → 展開為 N 列；無發票（如業務員公出）→ 1 列，發票欄位皆為 null。
/// </summary>
public sealed record PaymentExportRowDto(
    int       PaymentRequestId,
    string    RequestNo,
    string    EmployeeName,
    string    Type,
    string    ProjectCode,
    string    ProjectName,
    string    ApprovalStatus,
    DateTime  CreatedAt,
    DateTime? PaidAt,
    decimal   PaymentTotalAmount,
    string?   InvoiceNo,
    string?   InvoiceItemName,
    DateTime? InvoiceDate,
    decimal?  InvoiceAmount);
