namespace Jabez.Api.Models.Dtos;

/// <summary>專案水位表 DTO</summary>
/// <param name="ProjectId">專案 ID</param>
/// <param name="ProjectCode">專案代碼</param>
/// <param name="ProjectName">專案名稱</param>
/// <param name="Status">專案狀態</param>
/// <param name="DepartmentName">所屬部門名稱</param>
/// <param name="ContractAmount">契約金額（null 代表未設定）</param>
/// <param name="BusinessAmount">業務執行金額（null 代表未設定）</param>
/// <param name="RemainingAmount">系統導入時的契約剩餘預算（null 代表未設定）</param>
/// <param name="DisbursedAmount">
/// 已動支（四種支出來源加總）：
/// 1) 請款已撥分期金額（PaymentRequestInstallment.PaidAt IS NOT NULL，且 PaymentRequest 非 draft）
/// 2) 已核准預支沖銷 GrandTotal（透過 AdvanceRequest.ProjectId 回扣專案）
/// 3) 出差請款已撥分期金額（TravelPaymentRequestInstallment.PaidAt IS NOT NULL，且 TravelPaymentRequest 非 draft）
/// 4) 已核准出差沖銷 GrandTotal（透過 TravelRequest.ProjectId 回扣專案）
/// </param>
/// <param name="PreImportUsedAmount">系統導入前已使用金額 = (ContractAmount - RemainingAmount)，clamped &gt;= 0</param>
/// <param name="Percentage">已動支金額佔業務執行金額百分比（BusinessAmount 為 null 或 0 時回傳 null）</param>
/// <param name="TotalPercentage">(已動支金額 + 系統導入前已使用金額) 佔契約金額百分比（ContractAmount 為 null 或 0 時回傳 null）</param>
public sealed record ProjectWaterLevelDto(
    int      ProjectId,
    string   ProjectCode,
    string   ProjectName,
    string   Status,
    string?  DepartmentName,
    decimal? ContractAmount,
    decimal? BusinessAmount,
    decimal? RemainingAmount,
    decimal  DisbursedAmount,
    decimal  PreImportUsedAmount,
    decimal? Percentage,
    decimal? TotalPercentage);
