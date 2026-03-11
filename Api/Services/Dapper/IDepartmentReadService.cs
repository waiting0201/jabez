using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IDepartmentReadService
{
    Task<IEnumerable<DepartmentDto>> GetAllAsync();
    Task<DepartmentDto?>             GetByIdAsync(int id);
}
