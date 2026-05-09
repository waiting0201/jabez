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
/// <param name="PaymentAmount">所有非 draft 請款申請的 TotalAmount 加總</param>
/// <param name="PaidAmount">已付款（PaidAt IS NOT NULL）請款申請的 TotalAmount 加總</param>
/// <param name="PreImportUsedAmount">系統導入前已使用金額 = (ContractAmount - RemainingAmount)，clamped &gt;= 0</param>
/// <param name="Percentage">請款金額佔業務執行金額百分比（BusinessAmount 為 null 或 0 時回傳 null）</param>
/// <param name="TotalPercentage">(請款金額 + 系統導入前已使用金額) 佔契約金額百分比（ContractAmount 為 null 或 0 時回傳 null）</param>
public sealed record ProjectWaterLevelDto(
    int      ProjectId,
    string   ProjectCode,
    string   ProjectName,
    string   Status,
    string?  DepartmentName,
    decimal? ContractAmount,
    decimal? BusinessAmount,
    decimal? RemainingAmount,
    decimal  PaymentAmount,
    decimal  PaidAmount,
    decimal  PreImportUsedAmount,
    decimal? Percentage,
    decimal? TotalPercentage);
