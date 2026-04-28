namespace Jabez.Api.Services;

/// <summary>
/// LINE 推播結果。失敗時 ErrorCategory + ErrorMessage 提供分類與原始訊息給打卡提醒紀錄使用。
/// 既有 6 處 ApprovalNotificationService 呼叫不取值，改成此 record 仍編譯相容。
/// </summary>
/// <param name="Success">HTTP 2xx 視為成功</param>
/// <param name="HttpStatusCode">LINE API 回應狀態碼（網路錯誤時為 null）</param>
/// <param name="ErrorCategory">not_friend / token_invalid / rate_limited / network_error / unknown / system_error</param>
/// <param name="ErrorMessage">截斷至 500 字的錯誤訊息</param>
public sealed record PushResult(
    bool    Success,
    int?    HttpStatusCode = null,
    string? ErrorCategory  = null,
    string? ErrorMessage   = null);
