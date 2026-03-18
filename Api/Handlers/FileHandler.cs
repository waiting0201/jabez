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
    private const string SignatureContainer = "signatures";

    /// <summary>
    /// 代理讀取簽名檔圖片。
    /// 路由：GET /files/signatures/{fileName}
    /// 此端點不需要 JWT（PDF 匯出時需要直接 fetch，無 Authorization header）。
    /// </summary>
    public async Task<IActionResult> GetSignatureAsync(string fileName)
    {
        // 防止路徑穿越攻擊（Path Traversal）
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains('/') || fileName.Contains('\\'))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid file name."));

        var result = await blob.DownloadAsync(SignatureContainer, fileName);
        if (result is null)
            return new NotFoundObjectResult(ApiResponse.Fail("File not found."));

        var (content, contentType) = result.Value;

        // 確保 Content-Type 為圖片類型，避免意外回傳非預期格式
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            contentType = "application/octet-stream";

        return new FileStreamResult(content, contentType);
    }
}
