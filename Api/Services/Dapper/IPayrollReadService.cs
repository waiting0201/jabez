using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IPayrollReadService
{
    /// <summary>計算指定月份薪資；employeeId 非 null 時只算該員工（供 /me/payroll 自助查詢）。</summary>
    Task<MonthlyPayrollDto> CalculateMonthlyPayrollAsync(int year, int month, Guid? employeeId = null);
}
