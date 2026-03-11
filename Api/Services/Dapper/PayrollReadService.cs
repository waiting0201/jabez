using Dapper;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class PayrollReadService(IDbConnection db) : IPayrollReadService
{
    public async Task<MonthlyPayrollDto> CalculateMonthlyPayrollAsync(int year, int month)
    {
        var firstDay = new DateTime(year, month, 1);
        var lastDay  = firstDay.AddMonths(1).AddDays(-1);

        // 1. 查詢所有在職員工（有底薪、未離職或離職日在該月之後）
        const string employeeSql = """
            SELECT u.Id AS EmployeeId, u.Name AS EmployeeName,
                   d.Name AS DepartmentName, jt.Name AS JobTitleName,
                   u.HireDate, u.BaseSalary
            FROM Users u
            LEFT JOIN Departments d  ON u.DepartmentId = d.Id
            LEFT JOIN JobTitles jt   ON u.JobTitleId = jt.Id
            WHERE u.IsSuperAdmin = 0
              AND u.Status = 'active'
              AND u.BaseSalary IS NOT NULL
              AND u.BaseSalary > 0
              AND (u.ResignDate IS NULL OR u.ResignDate >= @FirstDay)
            ORDER BY u.Name
            """;

        // 2. 查詢該月已核准的假日出差天數
        const string travelSql = """
            SELECT tr.EmployeeId,
                   SUM(DATEDIFF(DAY, tr.StartDate, tr.EndDate) + 1) AS TotalDays
            FROM TravelRequests tr
            WHERE tr.IsHolidayTravel = 1
              AND tr.ApprovalStatus = 'approved'
              AND tr.StartDate <= @LastDay
              AND tr.EndDate >= @FirstDay
            GROUP BY tr.EmployeeId
            """;

        // 3. 查詢所有勞健保級距
        const string bracketSql = """
            SELECT SalaryBracket, LaborInsuranceEmployee, HealthInsuranceEmployee
            FROM InsuranceBrackets
            ORDER BY SalaryBracket ASC
            """;

        var employees = (await db.QueryAsync<dynamic>(employeeSql, new { FirstDay = firstDay })).ToList();
        var travelDays = (await db.QueryAsync<dynamic>(travelSql, new { FirstDay = firstDay, LastDay = lastDay }))
            .ToDictionary(r => (Guid)r.EmployeeId, r => (int)r.TotalDays);
        var brackets = (await db.QueryAsync<dynamic>(bracketSql)).ToList();

        var results = new List<EmployeePayrollDto>();
        foreach (var emp in employees)
        {
            decimal baseSalary  = (decimal)emp.BaseSalary;
            decimal dailySalary = Math.Round(baseSalary / 30m, 0);

            int holidayDays = travelDays.TryGetValue((Guid)emp.EmployeeId, out var days) ? days : 0;
            decimal holidayAllowance = dailySalary * holidayDays;

            // 查找級距：第一個 SalaryBracket >= BaseSalary 的級距，若無則取最高級距
            var bracket = brackets.FirstOrDefault(b => (decimal)b.SalaryBracket >= baseSalary)
                       ?? brackets.LastOrDefault();

            decimal laborIns  = bracket is not null ? (decimal)bracket.LaborInsuranceEmployee  : 0m;
            decimal healthIns = bracket is not null ? (decimal)bracket.HealthInsuranceEmployee : 0m;

            decimal netSalary = baseSalary + holidayAllowance - laborIns - healthIns;

            results.Add(new EmployeePayrollDto(
                (Guid)emp.EmployeeId,
                (string)emp.EmployeeName,
                (string?)emp.DepartmentName,
                (string?)emp.JobTitleName,
                (DateTime?)emp.HireDate,
                baseSalary,
                dailySalary,
                holidayDays,
                holidayAllowance,
                laborIns,
                healthIns,
                netSalary));
        }

        return new MonthlyPayrollDto(
            year, month, results,
            results.Sum(r => r.BaseSalary),
            results.Sum(r => r.HolidayAllowance),
            results.Sum(r => r.LaborInsurance),
            results.Sum(r => r.HealthInsurance),
            results.Sum(r => r.NetSalary));
    }
}
