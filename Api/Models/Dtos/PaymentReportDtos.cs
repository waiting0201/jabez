namespace Jabez.Api.Models.Dtos;

/// <summary>
/// 款項統計列表列。每筆主表附帶 Items 子表明細（無明細時為空陣列）。
/// 前端以 Items 展開為多列：請款層欄位只顯示在第一列，明細層欄位每列獨立。
/// </summary>
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
    DateTime  SubmittedAt,   // 送簽日期（申請日期）；報表只含非草稿，必有值
    List<PaymentReportItemDto> Items);

/// <summary>
/// 款項統計明細列（4 欄語意依 category 由前端對應）：
/// - payment / writeoff / travel-payment / travel / travel-writeoff → Col1=發票號碼、Col3Date=發票日期
/// - advance → Col1=類別、Col3Text=數量(字串)
/// </summary>
public sealed record PaymentReportItemDto(
    string?   Col1,
    string?   ItemName,
    string?   Col3Text,
    DateTime? Col3Date,
    decimal?  Amount);

/// <summary>
/// 款項統計匯出列：主表 LEFT JOIN 子表，一列一明細。
/// ItemCol1 / ItemName / ItemCol3 / ItemAmount 4 欄語意依 category 由前端對應表頭。
/// </summary>
public sealed record PaymentExportRowDto(
    int       ParentId,
    string    RequestNo,
    string    EmployeeName,
    string    Type,
    string    ProjectCode,
    string    ProjectName,
    string    ApprovalStatus,
    DateTime  SubmittedAt,   // 送簽日期（申請日期）；報表只含非草稿，必有值
    DateTime? PaidAt,
    decimal   PaymentTotalAmount,
    string?   ItemCol1,
    string?   ItemName,
    string?   ItemCol3Text,
    DateTime? ItemCol3Date,
    decimal?  ItemAmount);
