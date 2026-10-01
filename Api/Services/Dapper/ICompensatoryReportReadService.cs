using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;

namespace Jabez.Api.Services.Dapper;

public interface ICompensatoryReportReadService
{
    Task<IReadOnlyList<CompensatoryEmployeeRaw>> GetEmployeesAsync(ProjectAccessScope scope, DateTime dateFrom, DateTime dateTo);
    Task<IReadOnlyList<CompensatoryOvertimeRaw>> GetPeriodOvertimesAsync(ProjectAccessScope scope, DateTime dateFrom, DateTime dateTo);
}
