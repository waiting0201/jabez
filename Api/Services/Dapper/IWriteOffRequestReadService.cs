using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IWriteOffRequestReadService
{
    Task<PagedResult<WriteOffRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null);
    Task<WriteOffRequestDto?>             GetByIdAsync(int id);
    Task<WriteOffRequestDto[]>            GetByAdvanceIdAsync(int advanceRequestId);
}
