namespace Jabez.Api.Models.Dtos;

/// <summary>單一員工的月薪計算結果</summary>
public sealed record EmployeePayrollDto(
    Guid      EmployeeId,
    string    EmployeeName,
    string?   DepartmentName,
    string?   JobTitleName,
    DateTime? HireDate,
    decimal   BaseSalary,
    decimal   DailySalary,
    int       HolidayTravelDays,
    decimal   HolidayAllowance,
    decimal   LaborInsurance,
    decimal   HealthInsurance,
    decimal   NetSalary);

/// <summary>整月薪資計算回傳</summary>
public sealed record MonthlyPayrollDto(
    int Year,
    int Month,
    IEnumerable<EmployeePayrollDto> Employees,
    decimal TotalBaseSalary,
    decimal TotalHolidayAllowance,
    decimal TotalLaborInsurance,
    decimal TotalHealthInsurance,
    decimal TotalNetSalary);
