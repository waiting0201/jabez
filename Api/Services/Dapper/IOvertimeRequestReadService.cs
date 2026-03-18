using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IOvertimeRequestReadService
{
    Task<IEnumerable<OvertimeRequestDto>> GetAllAsync();
    Task<PagedResult<OvertimeRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null);
    Task<OvertimeRequestDto?>             GetByIdAsync(int id);

    /// <summary>
    /// 依狀態、加班日期與員工 ID 篩選加班申請（用於打卡頁面選取已核准申請）。
    /// 任一參數為 null 時忽略該條件。
    /// </summary>
    Task<IEnumerable<OvertimeRequestDto>> GetFilteredAsync(string? status, DateOnly? date, Guid? employeeId);
}
