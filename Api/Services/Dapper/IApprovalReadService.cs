using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IApprovalReadService
{
    Task<IEnumerable<ApprovalItemDto>> GetAllAsync();
    Task<ApprovalItemDto?>             GetByIdAsync(int id);

    /// <summary>
    /// 取得指定 ApplicationType 的啟用中流程（精簡版），供申請表單判斷是否有指定審核步驟。
    /// 不含部門 / 職稱等敏感設定，因此可開放給未持有 approvals:read 權限的一般員工呼叫。
    /// </summary>
    Task<ApprovalFlowSummaryDto?> GetActiveByTypeAsync(string applicationType);
}
