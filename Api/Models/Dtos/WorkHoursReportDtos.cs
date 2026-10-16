namespace Jabez.Api.Models.Dtos;

/// <summary>
/// 工時總表：一列 ＝ (員工, 專案)。ProjectId 為 null 代表「未分配」
/// （已打下班卡但沒有專案分配，例如系統自動補下班卡），整天以 8 小時計。
/// </summary>
public sealed record WorkHoursRowDto(
    string?  DepartmentName,
    Guid     EmployeeId,
    string   EmployeeName,
    int?     ProjectId,
    string?  ProjectCode,
    string?  ProjectName,
    int      Days,       // 貢獻的打卡紀錄天數
    decimal  Hours);     // Σ 8 × 佔比% ÷ 100

/// <summary>逐日明細：一列 ＝ (日期, 員工, 專案分配)；未分配的日子一天一列、Percentage 為 null、Hours 為 8。</summary>
public sealed record WorkHoursDailyRowDto(
    string   Date,       // yyyy-MM-dd
    string?  DepartmentName,
    Guid     EmployeeId,
    string   EmployeeName,
    int?     ProjectId,
    string?  ProjectCode,
    string?  ProjectName,
    decimal? Percentage,
    decimal  Hours,
    bool     IsAutoClockOut);

/// <summary>ReadService 原料列（尚未乘 8 小時）：Percentage 為 null ＝ 該打卡紀錄沒有任何分配。</summary>
public sealed record WorkHoursRaw(
    DateTime Date,
    string?  DepartmentName,
    Guid     EmployeeId,
    string   EmployeeName,
    int?     ProjectId,
    string?  ProjectCode,
    string?  ProjectName,
    decimal? Percentage,
    bool     IsAutoClockOut);
