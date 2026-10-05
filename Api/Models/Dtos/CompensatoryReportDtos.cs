namespace Jabez.Api.Models.Dtos;

/// <summary>
/// 加班補休時數總表：一位員工一列。
/// 「期間」欄位只看查詢區間內的加班日 / 補休起日；<see cref="AvailableHours"/> 則是**截至今日**的待補休
/// （與個人資訊頁、請假表單的補休餘額同一公式，見 CompensatoryBalance）。
/// </summary>
public sealed record CompensatoryReportRowDto(
    Guid     EmployeeId,
    string   EmployeeName,
    int?     DepartmentId,
    string?  DepartmentName,
    decimal  Tier134Hours,       // 期間取得：1~2 小時（×1.34）
    decimal  Tier167Hours,       // 期間取得：3~8 小時（×1.67；平日第 3 小時起皆屬此級）
    decimal  Tier267Hours,       // 期間取得：9 小時起（×2.67，僅休假日）
    decimal  PeriodEarnedHours,  // 期間取得合計（申請時數；國定假日前 8 小時不列級距，故可能大於三級距加總）
    decimal  PeriodUsedHours,    // 期間已休（送簽中 + 已核准的補休假）
    decimal  AvailableHours,     // 截至今日待補休
    decimal? Amount);            // 期間取得換算金額（Σ 級距時數 × 倍率 × 現行時薪）；無 reports-overtime:amount 者為 null

/// <summary>ReadService 原料：員工 + 全期間補休帳（算餘額用）+ 期間已休。</summary>
public sealed record CompensatoryEmployeeRaw(
    Guid     EmployeeId,
    string   EmployeeName,
    int?     DepartmentId,
    string?  DepartmentName,
    bool     IsShiftWorker,
    decimal? BaseSalary,
    decimal  OpeningHours,
    decimal  EarnedHours,
    decimal  UsedHours,
    decimal  PeriodUsedHours);

/// <summary>ReadService 原料：期間內已核准、補償方式為補休的加班單。BillableHours＝給付基準（ISNULL(SettledHours, EstimatedHours)）。</summary>
public sealed record CompensatoryOvertimeRaw(
    Guid     EmployeeId,
    DateTime OvertimeDate,
    decimal  BillableHours);
