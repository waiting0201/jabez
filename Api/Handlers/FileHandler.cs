using Jabez.Api.Common;
using Jabez.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// 檔案代理 Handler。
/// 透過後端 API 代理讀取私有 Blob Storage，避免前端直接存取 Blob URL 遭到 403 或 CORS 問題。
/// </summary>
public sealed class FileHandler(IBlobStorageService blob)
{
    private const string SignatureContainer        = "signatures";
    private const string AvatarContainer           = "avatars";
    private const string IndigenousProofContainer  = "indigenous-proofs";

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
    public async Task<IActionResult> GetIndigenousProofAsync(string fileName)
    {
        if (!IsSafeFileName(fileName))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid file name."));

        var result = await blob.DownloadAsync(IndigenousProofContainer, fileName);
        if (result is null)
            return new NotFoundObjectResult(ApiResponse.Fail("File not found."));

        var (content, contentType) = result.Value;

        // 僅允許圖片或 PDF；其他型別降級為 octet-stream 以避免意外洩漏內容
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            && !contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            contentType = "application/octet-stream";

        return new FileStreamResult(content, contentType);
    }

    private async Task<IActionResult> GetImageAsync(string container, string fileName)
    {
        if (!IsSafeFileName(fileName))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid file name."));

        var result = await blob.DownloadAsync(container, fileName);
        if (result is null)
            return new NotFoundObjectResult(ApiResponse.Fail("File not found."));

        var (content, contentType) = result.Value;

        // 確保 Content-Type 為圖片類型，避免意外回傳非預期格式
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            contentType = "application/octet-stream";

        return new FileStreamResult(content, contentType);
    }

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
