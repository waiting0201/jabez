namespace Jabez.Api.Services;

/// <summary>LINE Platform API 操作介面。</summary>
public interface ILineService
{
    /// <summary>用 authorization code 換取 id_token，驗證後回傳 LINE userId。</summary>
    Task<string?> ExchangeCodeForUserIdAsync(string code, string redirectUri);

    /// <summary>
    /// 推播 Flex Message 給指定 LINE userId。
    /// 回傳是否成功送達（HTTP 2xx 視為成功；非 2xx 已寫 log 但不丟例外）。
    /// </summary>
    Task<bool> PushMessageAsync(string lineUserId, object messagePayload);

    /// <summary>
    /// 檢查用戶是否為 OA Bot 好友（呼叫 GET /v2/bot/profile/{userId}）。
    /// 200 = 是好友（推播可用）；404 = 未加好友或已封鎖（推播不可用）。
    /// </summary>
    Task<bool> IsBotFriendAsync(string lineUserId);
}
