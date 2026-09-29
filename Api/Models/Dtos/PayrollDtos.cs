namespace Jabez.Api.Models.Dtos;

/// <summary>單一員工的月薪計算結果</summary>
public sealed record EmployeePayrollDto(
    Guid      EmployeeId,
    string    EmployeeName,
    string?   Email,
    bool      SendPaySlip,
    string?   DepartmentName,
    string?   JobTitleName,
    DateTime? HireDate,
    decimal   BaseSalary,
    decimal   MealAllowance,
    decimal   OvertimePay,
    decimal   DailySalary,
    decimal   HolidayTravelDays,   // 半天以 0.5 計（假日活動參與人員可逐日勾上/下半天）
    decimal   HolidayAllowance,
    decimal   OtherAddition,
    string?   OtherAdditionNote,
    decimal   LaborInsurance,
    decimal   HealthInsurance,
    decimal   PersonalLeaveDays,
    decimal   PersonalLeaveDeduction,
    decimal   SickLeaveDays,
    decimal   SickLeaveDeduction,
    decimal   MenstrualLeaveDays,
    decimal   MenstrualLeaveDeduction,
    decimal   FamilyCareLeaveDays,
    decimal   FamilyCareLeaveDeduction,
    decimal   OtherDeduction,
    string?   OtherDeductionNote,
    string?   Note,
    decimal   NetSalary,
    LeaveDetailDto[]? LeaveDetails     = null,
    int               DependentCount   = 0,
    int               CappedDependentCount = 0,
    // 加給（自動同步自最新 SalaryAdjustmentRecord，計入 NetSalary 的加項）
    decimal   OtherAllowanceAmount = 0m,
    decimal   AdjustmentDifference = 0m,
    // 勞退自提（%，直接欄位、非覆寫，計入 NetSalary 的扣項）
    decimal?  LaborPensionSelfContributionRate = null,
    decimal   LaborPensionSelfDeduction        = 0m,
    // 育嬰留職停薪：該月留停日曆天數。> 0 代表底薪與各加給已按「在職天數 ÷ 30」折減；
    // 整月留停者不會出現在名單中（見 PayrollReadService）
    decimal   ParentalLeaveDays = 0m,
    // 加班申請試算加班費（上月加班日、已核准且選「加班費」的申請單快照合計）。
    // 與 OvertimePay（Users 表手填的固定加班費）**併存、不取代**，兩者是不同來源的兩筆錢。
    decimal   CalculatedOvertimePay   = 0m,
    decimal   CalculatedOvertimeHours = 0m,
    // 國定假日出勤加倍工資（四週彈性工時 §6.3.1）：上月國定假日出勤天數 × 日薪。
    // 「出勤」＝當日有上班打卡 ∪ 有已核准加班單，**依日期去重**（同日只加發一次）。
    // 出勤即加發一日、不按時數比例折算。切換日之前恆為 0。
    // ⚠ 新欄一律**加在最末端**：本 record 有 39 個位置參數且多為 decimal，
    //   插在中間會靜默錯位（編譯器擋不住），見 CLAUDE.md 薪資欄位連動規則。
    int       PublicHolidayWorkDays   = 0,
    decimal   PublicHolidayDoublePay  = 0m,
    // 補休未休完加班津貼（四週彈性工時 §7.5）：上月到期的補休 lot，
    // 依 lot 上的**原始加班費率快照** × 現行時薪換算。Note 為薪資單上的項目名稱（含民國年期間）。
    decimal   CompensatorySettlementHours  = 0m,
    decimal   CompensatorySettlementAmount = 0m,
    string?   CompensatorySettlementNote   = null);

/// <summary>請假明細（用於薪資頁面顯示）</summary>
public sealed record LeaveDetailDto(
    string   LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    decimal  Hours);

/// <summary>整月薪資計算回傳</summary>
public sealed record MonthlyPayrollDto(
    int Year,
    int Month,
    IEnumerable<EmployeePayrollDto> Employees,
    decimal TotalBaseSalary,
    decimal TotalMealAllowance,
    decimal TotalOvertimePay,
    decimal TotalHolidayAllowance,
    decimal TotalOtherAddition,
    decimal TotalLaborInsurance,
    decimal TotalHealthInsurance,
    decimal TotalPersonalLeaveDeduction,
    decimal TotalSickLeaveDeduction,
    decimal TotalMenstrualLeaveDeduction,
    decimal TotalFamilyCareLeaveDeduction,
    decimal TotalOtherDeduction,
    decimal TotalNetSalary,
    decimal TotalOtherAllowance       = 0m,
    decimal TotalAdjustmentDifference = 0m,
    decimal TotalLaborPensionSelfDeduction = 0m,
    decimal TotalParentalLeaveDays         = 0m,
    decimal TotalCalculatedOvertimePay      = 0m,
    decimal TotalPublicHolidayDoublePay     = 0m,
    decimal TotalCompensatorySettlementAmount = 0m);

/// <summary>
/// 員工自助查詢：單月薪資紀錄（Payroll 為當月即時重算結果，非月結快照）
/// </summary>
public sealed record MyPayrollMonthDto(
    int  Year,
    int  Month,
    bool IsCurrentMonth,
    EmployeePayrollDto Payroll);

/// <summary>員工自助查詢：近 N 個月薪資紀錄（新到舊）</summary>
public sealed record MyPayrollHistoryDto(IEnumerable<MyPayrollMonthDto> Months);

/// <summary>薪資調整新增/更新請求</summary>
public sealed record PayrollAdjustmentRequest(
    decimal  OtherAddition,
    string?  OtherAdditionNote,
    decimal  OtherDeduction,
    string?  OtherDeductionNote,
    string?  Note);

/// <summary>薪資調整回傳</summary>
public sealed record PayrollAdjustmentDto(
    int      Id,
    Guid     EmployeeId,
    int      Year,
    int      Month,
    decimal  OtherAddition,
    string?  OtherAdditionNote,
    decimal  OtherDeduction,
    string?  OtherDeductionNote,
    string?  Note);
