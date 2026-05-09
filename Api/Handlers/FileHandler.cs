using Jabez.Api.Common;
using Jabez.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Handlers;

/// <summary>
/// 檔案代理 Handler。
/// 透過後端 API 代理讀取私有 Blob Storage，避免前端直接存取 Blob URL 遭到 403 或 CORS 問題。
/// </summary>
public sealed class FileHandler(IBlobStorageService blob, ILogger<FileHandler> logger)
{
    private const string SignatureContainer        = "signatures";
    private const string AvatarContainer           = "avatars";
    private const string IndigenousProofContainer  = "indigenous-proofs";
    private const string LowIncomeProofContainer   = "low-income-proofs";
    private const string DisabledProofContainer    = "disabled-proofs";
    private const string IdCardContainer           = "id-cards";
    private const string EducationProofContainer   = "education-proofs";

    /// <summary>
    /// 代理讀取簽名檔圖片。
    /// 路由：GET /files/signatures/{fileName}
    /// 此端點不需要 JWT（PDF 匯出時需要直接 fetch，無 Authorization header）。
    /// </summary>
    public Task<IActionResult> GetSignatureAsync(string fileName)
        => GetImageAsync(SignatureContainer, fileName);

    /// <summary>
    /// 代理讀取頭像圖片。
    /// 路由：GET /files/avatars/{fileName}
    /// 此端點不需要 JWT（topbar 顯示頭像時不帶 Authorization header）。
    /// </summary>
    public Task<IActionResult> GetAvatarAsync(string fileName)
        => GetImageAsync(AvatarContainer, fileName);

    /// <summary>
    /// 代理讀取原住民證明文件（圖片或 PDF）。
    /// 路由：GET /files/indigenous-proofs/{fileName}
    /// 此端點需要 JWT + users:read 權限（HR 敏感 PII，僅人事管理員可檢視）。
    /// </summary>
    public Task<IActionResult> GetIndigenousProofAsync(string fileName)
        => GetFileAsync(IndigenousProofContainer, fileName, IsImageOrPdf);

    /// <summary>
    /// 代理讀取低收入戶證明文件（圖片或 PDF）。
    /// 路由：GET /files/low-income-proofs/{fileName}
    /// 此端點需要 JWT + users:read 權限（HR 敏感 PII，僅人事管理員可檢視）。
    /// </summary>
    public Task<IActionResult> GetLowIncomeProofAsync(string fileName)
        => GetFileAsync(LowIncomeProofContainer, fileName, IsImageOrPdf);

    /// <summary>
    /// 代理讀取身心障礙手冊證明文件（圖片或 PDF）。
    /// 路由：GET /files/disabled-proofs/{fileName}
    /// 此端點需要 JWT + users:read 權限（HR 敏感 PII，僅人事管理員可檢視）。
    /// </summary>
    public Task<IActionResult> GetDisabledProofAsync(string fileName)
        => GetFileAsync(DisabledProofContainer, fileName, IsImageOrPdf);

    /// <summary>
    /// 代理讀取身分證影本（圖片或 PDF）。
    /// 路由：GET /files/id-cards/{fileName}
    /// 此端點需要 JWT + users:read 權限（HR 敏感 PII，僅人事管理員可檢視）。
    /// </summary>
    public Task<IActionResult> GetIdCardAsync(string fileName)
        => GetFileAsync(IdCardContainer, fileName, IsImageOrPdf);

    /// <summary>
    /// 代理讀取最高學歷證明文件（圖片或 PDF）。
    /// 路由：GET /files/education-proofs/{fileName}
    /// 此端點需要 JWT + users:read 權限（HR 敏感 PII，僅人事管理員可檢視）。
    /// </summary>
    public Task<IActionResult> GetEducationProofAsync(string fileName)
        => GetFileAsync(EducationProofContainer, fileName, IsImageOrPdf);

    private Task<IActionResult> GetImageAsync(string container, string fileName)
        => GetFileAsync(container, fileName, IsImage);

    /// <summary>
    /// 共用代理：取出 Blob 後驗證 Content-Type 是否在預期清單。
    /// 不在清單時不靜默降級為 octet-stream（會讓前端拿到無法顯示的檔案而難以發現問題），
    /// 改為 LogError + 500，將「container 內存在意料外的檔案」這種資料異常立刻浮上來。
    /// </summary>
    private async Task<IActionResult> GetFileAsync(string container, string fileName, Func<string, bool> isAllowed)
    {
        if (!IsSafeFileName(fileName))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid file name."));

        var result = await blob.DownloadAsync(container, fileName);
        if (result is null)
            return new NotFoundObjectResult(ApiResponse.Fail("File not found."));

        var (content, contentType) = result.Value;

        if (!isAllowed(contentType))
        {
            logger.LogError(
                "Blob 內含預期外的 Content-Type：container={Container} blob={Blob} contentType={Type} — 上傳路徑應已擋下，請檢查資料完整性。",
                container, fileName, contentType);
            await content.DisposeAsync();
            return new ObjectResult(ApiResponse.Fail("檔案格式不符預期，請聯絡系統管理員。"))
                { StatusCode = 500 };
        }

        return new FileStreamResult(content, contentType);
    }

    private static bool IsImage(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    private static bool IsImageOrPdf(string contentType) =>
        IsImage(contentType)
        || contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

    // 防止路徑穿越攻擊（Path Traversal）
    // 拒絕：URL 編碼的分隔符與 .. 序列、原始分隔符、控制字元、空白檔名
    private static bool IsSafeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // 解碼後再次檢查，避免 %2f / %5c / %2e%2e 繞過
        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(fileName);
        }
        catch
        {
            return false;
        }

        return !ContainsTraversal(fileName) && !ContainsTraversal(decoded);
    }

    private static bool ContainsTraversal(string value) =>
        value.Contains('/')
        || value.Contains('\\')
        || value.Contains("..", StringComparison.Ordinal)
        || value.Contains('\0')
        || value.Any(char.IsControl);
}
