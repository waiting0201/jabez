using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface ILeaveRequestReadService
{
    Task<IEnumerable<LeaveRequestDto>>    GetAllAsync();
    Task<PagedResult<LeaveRequestDto>>    GetPagedAsync(int page, int pageSize, Guid? userId = null);
    Task<LeaveRequestDto?>                GetByIdAsync(int id);

    // 查詢同員工 [startDate, endDate) 內、狀態為 draft/pending/approved 的重疊申請（編輯時可用 excludeId 排除自身）
    Task<IEnumerable<OverlappingLeaveRequestDto>> GetOverlappingRequestsAsync(
        Guid employeeId, DateTime startDate, DateTime endDate, int? excludeId = null);
}
