using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services;

/// <summary>LINE Platform API 操作介面。</summary>
public interface ILineService
{
    /// <summary>用 authorization code 換取 id_token，驗證後回傳 LINE userId。</summary>
    Task<string?> ExchangeCodeForUserIdAsync(string code, string redirectUri);

    /// <summary>
    /// 推播 Flex Message 給指定 LINE userId。
    /// 回傳 <see cref="PushResult"/>，包含成功旗標、HTTP 狀態碼與失敗分類。HTTP 2xx 視為成功；非 2xx 已寫 log 但不丟例外。
    /// </summary>
    Task<PushResult> PushMessageAsync(string lineUserId, object messagePayload);

    /// <summary>
    /// 檢查用戶是否為 OA Bot 好友（呼叫 GET /v2/bot/profile/{userId}）。
    /// 200 = 是好友（推播可用）；404 = 未加好友或已封鎖（推播不可用）。
    /// </summary>
    Task<bool> IsBotFriendAsync(string lineUserId);

    /// <summary>
    /// 查詢本月 LINE Messaging API 推播用量（同時呼叫 quota + consumption 兩支 API 並合併）。
    /// 任一 API 失敗時回傳 null（caller 自行決定顯示方式）。
    /// </summary>
    Task<LineQuotaDto?> GetMessageQuotaAsync();
}
