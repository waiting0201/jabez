using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IVendorReadService
{
    Task<IEnumerable<VendorDto>>       GetAllAsync(string? search = null);
    Task<PagedResult<VendorDto>>       GetPagedAsync(int page, int pageSize, string? search = null);
    Task<IEnumerable<VendorLookupDto>> GetLookupAsync();
    Task<VendorDto?>                   GetByIdAsync(int id);
}
