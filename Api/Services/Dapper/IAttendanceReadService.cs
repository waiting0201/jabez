using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IAttendanceReadService
{
    Task<PagedResult<AttendanceRecordDto>> GetPagedAsync(int page, int pageSize,
        Guid? employeeId = null, int? year = null, int? month = null);
    Task<TodayAttendanceDto?>              GetTodayAsync(Guid userId);
}
