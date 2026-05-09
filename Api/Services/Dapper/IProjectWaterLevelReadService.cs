using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;

namespace Jabez.Api.Services.Dapper;

public interface IProjectWaterLevelReadService
{
    Task<IEnumerable<ProjectWaterLevelDto>> GetAllAsync(ProjectAccessScope scope, int? year = null, string? status = null);
}
