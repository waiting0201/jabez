using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IProjectWaterLevelReadService
{
    Task<IEnumerable<ProjectWaterLevelDto>> GetAllAsync();
}
