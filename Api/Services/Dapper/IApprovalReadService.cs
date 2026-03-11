using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IApprovalReadService
{
    Task<IEnumerable<ApprovalItemDto>> GetAllAsync();
    Task<ApprovalItemDto?>             GetByIdAsync(int id);
}
