using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IApprovalReadService
{
    Task<IEnumerable<ApprovalItemDto>> GetAllAsync();
    Task<ApprovalItemDto?>             GetByIdAsync(int id);

    /// <summary>
    /// 取得指定 ApplicationType「呼叫者部門實際會走」的啟用中流程（精簡版），供申請表單判斷是否有指定審核步驟。
    /// 解析順序：呼叫者部門 > 最近祖先部門（沿 ParentId 逐層往上）> 通用預設流程（DepartmentId == null）。
    /// 須與 ApprovalFlowService.ResolveApprovalItemIdAsync 的優先序一致。
    /// 不含部門 / 職稱等敏感設定，因此可開放給未持有 approvals:read 權限的一般員工呼叫。
    /// 回傳的 UseApplicantDesignated 為「對呼叫者而言的有效值」：步驟原生設定 OR 例外指定審核名單
    /// （ApprovalStepExceptions）命中 userId；userId 為 null 時退化為原生設定。
    /// </summary>
    Task<ApprovalFlowSummaryDto?> GetActiveByTypeAsync(string applicationType, int? departmentId, Guid? userId);
}
