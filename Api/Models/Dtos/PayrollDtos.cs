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
    int       HolidayTravelDays,
    decimal   HolidayAllowance,
    decimal   OtherAddition,
    string?   OtherAdditionNote,
    decimal   LaborInsurance,
    decimal   HealthInsurance,
    decimal   PersonalLeaveDays,
    decimal   PersonalLeaveDeduction,
    decimal   SickLeaveDays,
    decimal   SickLeaveDeduction,
    decimal   OtherDeduction,
    string?   OtherDeductionNote,
    string?   Note,
    decimal   NetSalary,
    LeaveDetailDto[]? LeaveDetails     = null,
    int               DependentCount   = 0,
    int               CappedDependentCount = 0);

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
    decimal TotalOtherDeduction,
    decimal TotalNetSalary);

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
