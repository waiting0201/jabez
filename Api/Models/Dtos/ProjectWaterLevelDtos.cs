namespace Jabez.Api.Models.Dtos;

/// <summary>專案水位表 DTO</summary>
/// <param name="ProjectId">專案 ID</param>
/// <param name="ProjectCode">專案代碼</param>
/// <param name="Status">專案狀態</param>
/// <param name="DepartmentName">所屬部門名稱</param>
/// <param name="BusinessAmount">業務金額（null 代表未設定）</param>
/// <param name="PaymentAmount">所有非 draft 請款申請的 TotalAmount 加總</param>
/// <param name="PaidAmount">已付款（PaidAt IS NOT NULL）請款申請的 TotalAmount 加總</param>
/// <param name="Percentage">請款金額佔業務金額百分比（BusinessAmount 為 null 或 0 時回傳 null）</param>
public sealed record ProjectWaterLevelDto(
    int      ProjectId,
    string   ProjectCode,
    string   Status,
    string?  DepartmentName,
    decimal? BusinessAmount,
    decimal  PaymentAmount,
    decimal  PaidAmount,
    decimal? Percentage);
