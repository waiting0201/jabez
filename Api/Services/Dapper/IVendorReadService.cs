using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IVendorReadService
{
    Task<IEnumerable<VendorDto>>       GetAllAsync();
    Task<IEnumerable<VendorLookupDto>> GetLookupAsync();
    Task<VendorDto?>                   GetByIdAsync(int id);
}
