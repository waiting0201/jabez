using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IInsuranceBracketReadService
{
    Task<IEnumerable<InsuranceBracketDto>> GetAllAsync();
    Task<InsuranceBracketDto?>             GetByIdAsync(int id);
    Task<InsuranceBracketDto?>             GetBySalaryAsync(decimal salary);
}
