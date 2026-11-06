namespace Jabez.Api.Common;

/// <summary>
/// HTTP 請求本文大小上限的單一真相（Program.cs 設定 Kestrel / FormOptions，ExceptionMiddleware 組錯誤訊息）。
/// <para>
/// 起因（2026-11-06 正式站）：worker 內建 Kestrel 預設只收 30MB，預支沖銷一次 90 筆明細各附一張發票圖即超過，
/// Kestrel 丟 <c>BadHttpRequestException</c> 後切斷連線，前端只看到「儲存失敗，請稍後再試」。
/// </para>
/// <para>
/// 值對齊 Azure Functions host 的預設上限（<c>FUNCTIONS_REQUEST_BODY_SIZE_LIMIT</c> = 100MB）：
/// 只調高 worker 端沒用，host 端會先擋；要再往上調須同時設定該 app setting。
/// 前端同一個數字在 <c>Admin/src/app/shared/utils/upload-error.ts</c>，兩處必須一起改。
/// </para>
/// </summary>
public static class RequestBodyLimits
{
    /// <summary>單次請求本文上限（bytes）</summary>
    public const long MaxBytes = 100L * 1024 * 1024;

    /// <summary>超過上限時回給前端的訊息</summary>
    public const string TooLargeMessage =
        "上傳檔案總量超過 100MB，請分成兩張以上的申請單送出，或改以較小的檔案（例如拍照而非掃描 PDF）上傳。";
}
