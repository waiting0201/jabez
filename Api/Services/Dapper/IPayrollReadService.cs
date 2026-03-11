using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IPayrollReadService
{
    Task<MonthlyPayrollDto> CalculateMonthlyPayrollAsync(int year, int month);
}
