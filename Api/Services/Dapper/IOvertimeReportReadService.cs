using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IOvertimeReportReadService
{
    Task<PagedResult<OvertimeReportDto>> GetPagedAsync(int page, int pageSize,
        Guid? employeeId = null, int? projectId = null, int? year = null, int? month = null);
}
