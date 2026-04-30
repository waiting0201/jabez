using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;

namespace Jabez.Api.Services.Dapper;

public interface IAttendanceReadService
{
    Task<PagedResult<AttendanceRecordDto>> GetPagedAsync(ProjectAccessScope scope, int page, int pageSize,
        Guid? employeeId = null, DateOnly? dateFrom = null, DateOnly? dateTo = null);
    Task<TodayAttendanceDto?>              GetTodayAsync(Guid userId);
}
