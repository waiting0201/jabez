using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IPermissionReadService
{
    Task<IEnumerable<PermissionDto>> GetAllAsync();
    Task<PermissionDto?>             GetByIdAsync(string id);
}
