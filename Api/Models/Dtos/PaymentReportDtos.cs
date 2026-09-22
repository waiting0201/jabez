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

/// <summary>
/// 待撥款清單的一列 —— **一期一列**（一張單有 3 期就 3 列），與上面「一單一列」的報表粒度不同。
///
/// 存在理由：預計撥款日是 installment 層級的欄位，用一單一列表達不出「哪一期到期」；
/// 財務排款時要回答的是「這週要撥哪幾筆、各多少錢」，不是「哪些單還沒撥完」。
/// </summary>
/// <param name="ApplicationType">
/// **snake_case**（payment_request / advance / travel / travel_payment / write_off）——
/// ⚠ 刻意不用報表那套 kebab（payment / travel-payment / writeoff）：
/// 這個值前端要直接拿去組簽核作業網址 `/admin/approval-tasks/{type}/{id}/review`，
/// 而該路由只認 <c>ApprovalTaskHandler.ValidAppTypes</c> 的 snake_case。映射在後端做掉，前端不再轉一次。
/// </param>
/// <param name="TotalInstallments">母單總期數</param>
/// <param name="PaidInstallments">母單已撥期數 —— 兩者合起來就是「已撥 1/3 期」的撥款進度</param>
/// <param name="ParentUnpaidAmount">母單尚未撥款的金額合計（不只本期）</param>
public sealed record DuePaymentRowDto(
    string    ApplicationType,
    int       ApplicationId,
    string?   RequestNo,
    string    EmployeeName,
    string?   ProjectCode,
    string?   ProjectName,
    int       InstallmentNo,
    int       TotalInstallments,
    int       PaidInstallments,
    DateTime  ExpectedDate,
    DateTime? PaidAt,
    decimal   Amount,
    string?   Note,
    decimal   ParentTotalAmount,
    decimal   ParentUnpaidAmount);
