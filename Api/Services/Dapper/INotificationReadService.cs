using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface INotificationReadService
{
    /// <summary>
    /// 取得使用者送出的進行中申請件數（依申請類型分組）。
    /// 「進行中」定義：ApprovalStatus IN ('pending', 'returned')
    ///   - pending：仍在簽核流程中
    ///   - returned：被退回需要重提
    /// 不含 draft（未送出）、approved（已核准）、rejected（已拒絕、終止）。
    /// </summary>
    /// <returns>key = 申請類型字串（與 ApprovalTaskHandler.ValidAppTypes 一致），value = 件數</returns>
    Task<IReadOnlyDictionary<string, int>> GetMyRequestCountsByTypeAsync(Guid userId);

    /// <summary>
    /// 取得使用者送出、且在 <paramref name="since"/> 之後曾有核准動作的「已核准」申請單。
    /// 供鈴鐺輪詢時跳「您的申請已核准」toast；去重由前端以 ApprovedAt 比對。
    /// </summary>
    Task<IReadOnlyList<RecentApprovalDto>> GetRecentApprovedMyRequestsAsync(Guid userId, DateTime since);
}
