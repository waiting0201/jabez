using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IAdvanceRequestReadService
{
    Task<PagedResult<AdvanceRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null);
    Task<AdvanceRequestDto?>             GetByIdAsync(int id);

    /// <summary>某預支單關聯的預審單（依 RoundNo、單號排序）。</summary>
    Task<IEnumerable<AdvanceLinkedPreReviewDto>> GetLinkedPreReviewsAsync(int advanceRequestId);

    /// <summary>預支表單可關聯的預審單：該申請人已核准的預審單（不限專案），附被【其他】預支單引用的單號。</summary>
    Task<IEnumerable<LinkablePreReviewDto>> GetLinkablePreReviewsAsync(Guid submitterId, int? excludeAdvanceRequestId);
}
