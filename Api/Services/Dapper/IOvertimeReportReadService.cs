using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;

namespace Jabez.Api.Services.Dapper;

public interface IOvertimeReportReadService
{
    Task<PagedResult<OvertimeReportDto>> GetPagedAsync(ProjectAccessScope scope, int page, int pageSize,
        Guid? employeeId = null, int? projectId = null, int? year = null, int? month = null);
}
