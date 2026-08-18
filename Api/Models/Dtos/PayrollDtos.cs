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
    decimal   ParentalLeaveDays = 0m);

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
    decimal TotalParentalLeaveDays         = 0m);

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
