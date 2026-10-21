using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IWriteOffRequestReadService
{
    Task<PagedResult<WriteOffRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null);
    Task<WriteOffRequestDto?>             GetByIdAsync(int id);
    Task<WriteOffRequestDto[]>            GetByAdvanceIdAsync(int advanceRequestId);
    /// <summary>批次取得各預支單底下的沖銷歷程（非 rejected，含 FinancePassed 與前次沖銷明細），依預支單 Id 分組。</summary>
    Task<Dictionary<int, WriteOffRoundDto[]>> GetHistoryForAdvancesAsync(IEnumerable<int> advanceRequestIds, int? currentId = null);
}
