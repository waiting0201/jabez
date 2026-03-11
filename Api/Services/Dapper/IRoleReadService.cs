using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IRoleReadService
{
    Task<IEnumerable<RoleDto>> GetAllAsync();
    Task<RoleDto?>             GetByIdAsync(string id);
}
