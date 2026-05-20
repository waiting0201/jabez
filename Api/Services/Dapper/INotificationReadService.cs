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
}
