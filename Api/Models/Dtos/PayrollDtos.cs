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
    decimal   PositionAllowance    = 0m,
    decimal   DutyAllowance        = 0m,
    decimal   OtherAllowanceAmount = 0m,
    decimal   AdjustmentDifference = 0m,
    decimal   OverseasAllowance    = 0m,
    // 勞退自提（%，直接欄位、非覆寫，計入 NetSalary 的扣項）
    decimal?  LaborPensionSelfContributionRate = null,
    decimal   LaborPensionSelfDeduction        = 0m);

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
    decimal TotalPositionAllowance    = 0m,
    decimal TotalDutyAllowance        = 0m,
    decimal TotalOtherAllowance       = 0m,
    decimal TotalAdjustmentDifference = 0m,
    decimal TotalOverseasAllowance    = 0m,
    decimal TotalLaborPensionSelfDeduction = 0m);

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
