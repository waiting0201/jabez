using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IJobTitleReadService
{
    Task<IEnumerable<JobTitleDto>> GetAllAsync();
    Task<JobTitleDto?>             GetByIdAsync(int id);
}
