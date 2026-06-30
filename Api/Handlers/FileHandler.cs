using Jabez.Api.Common;
using Jabez.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

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
    private const string VendorPassbookContainer   = "vendor-passbooks";
    private const string VendorIdCardContainer     = "vendor-id-cards";
    private const string QuotesContainer           = "quotes";
    private const string RequestAttachmentContainer = "request-attachments";

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

    /// <summary>
    /// 代理讀取廠商存摺封面（圖片或 PDF）。
    /// 路由：GET /files/vendor-passbooks/{fileName}
    /// 此端點需要 JWT，但不需特殊權限（一般檔案，與 avatars / signatures 同層）。
    /// </summary>
    public Task<IActionResult> GetVendorPassbookAsync(string fileName)
        => GetFileAsync(VendorPassbookContainer, fileName, IsImageOrPdf);

    /// <summary>
    /// 代理讀取廠商身分證影本（圖片或 PDF；個人工作室 / 外包顧問）。
    /// 路由：GET /files/vendor-id-cards/{fileName}
    /// 此端點需要 JWT + vendors:read 權限（身分證屬敏感 PII，僅廠商管理者可檢視）。
    /// </summary>
    public Task<IActionResult> GetVendorIdCardAsync(string fileName)
        => GetFileAsync(VendorIdCardContainer, fileName, IsImageOrPdf);

    /// <summary>
    /// 代理讀取預審 / 請款品項報價單（圖片或 PDF）。
    /// 路由：GET /files/quotes/{*path}
    /// 此端點需要 JWT，但不需特殊權限（一般業務檔案，與 avatars / signatures 同層）。
    /// blob 命名為 yyyy/MM/{guid}{ext}（含 '/'），故走允許子路徑的取檔核心。
    /// </summary>
    public Task<IActionResult> GetQuoteAsync(string path)
        => GetSubPathFileAsync(QuotesContainer, path, IsImageOrPdf);

    /// <summary>
    /// 代理讀取整單批次附件（圖片或 PDF；一般請款 / 預支沖銷 / 預審 共用）。
    /// 路由：GET /files/request-attachments/{*path}
    /// 此端點需要 JWT，但不需特殊權限（一般業務檔案，與 avatars / signatures 同層）。
    /// blob 命名為 yyyy/MM/{guid}{ext}（含 '/'），故走允許子路徑的取檔核心。
    /// </summary>
    public Task<IActionResult> GetRequestAttachmentAsync(string path)
        => GetSubPathFileAsync(RequestAttachmentContainer, path, IsImageOrPdf);

    // 員工可自助存取的 Blob 容器白名單（PII 類，但限制為「讀自己的」）
    private static readonly HashSet<string> SelfServiceContainers = new(StringComparer.OrdinalIgnoreCase)
    {
        "id-cards",
        "education-proofs",
        "indigenous-proofs",
        "low-income-proofs",
        "disabled-proofs",
        "avatars",
        "signatures",
    };

    /// <summary>
    /// 員工自助讀取自己的 PII 檔案。
    /// 路由：GET /me/files/{container}/{fileName}
    /// 此端點需要 JWT（登入即可），不需 users:read 等管理權限。
    /// 安全機制：
    ///   1. 白名單容器（SelfServiceContainers）：不在白名單一律 404，避免員工竄改 container 讀他人資料。
    ///   2. fileName 前綴必須以自身 userId 開頭（後接 '.' 或 '_'），避免員工竄改 fileName 讀其他人的檔案。
    ///   3. blob 命名規則（由上傳端保證）：
    ///        avatars / signatures / proofs  = {userId}{ext}
    ///        id-cards                       = {userId}_front{ext} / {userId}_back{ext}
    ///        education-proofs               = {userId}_education{ext}
    /// </summary>
    public async Task<IActionResult> GetMineAsync(HttpRequest req, string container, string fileName)
    {
        // 從 AppRouter 已驗證並寫入的 principal 取 userId
        var userIdStr = req.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return new UnauthorizedObjectResult(ApiResponse.Fail("Unauthorized.", "Invalid token claims."));

        // 白名單容器檢查
        if (!SelfServiceContainers.Contains(container))
            return new NotFoundObjectResult(ApiResponse.Fail("File not found."));

        // fileName 安全前綴檢查：必須以自身 userId 開頭（後接 '.' 或 '_'）
        // 範例合法：{guid}.png、{guid}_front.jpg、{guid}_back.pdf、{guid}_education.pdf
        var prefix = userId.ToString();
        var afterPrefix = fileName.Length > prefix.Length ? fileName[prefix.Length] : '\0';
        if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || (afterPrefix != '.' && afterPrefix != '_'))
        {
            return new ObjectResult(ApiResponse.Fail("存取被拒。")) { StatusCode = 403 };
        }

        return await GetFileAsync(container, fileName, IsImageOrPdf);
    }

    private Task<IActionResult> GetImageAsync(string container, string fileName)
        => GetFileAsync(container, fileName, IsImage);

    /// <summary>
    /// 共用代理（允許多段子路徑）：用於 blob name 含日期子路徑（yyyy/MM/{guid}{ext}）的容器。
    /// 與 <see cref="GetFileAsync"/> 行為一致，差別在於檔名驗證允許 '/'（仍阻擋 '..' / '\' / 控制字元 / 空白）。
    /// </summary>
    private async Task<IActionResult> GetSubPathFileAsync(string container, string path, Func<string, bool> isAllowed)
    {
        if (!IsSafeSubPath(path))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid file name."));

        var result = await blob.DownloadAsync(container, path);
        if (result is null)
            return new NotFoundObjectResult(ApiResponse.Fail("File not found."));

        var (content, contentType) = result.Value;

        if (!isAllowed(contentType))
        {
            logger.LogError(
                "Blob 內含預期外的 Content-Type：container={Container} blob={Blob} contentType={Type} — 上傳路徑應已擋下，請檢查資料完整性。",
                container, path, contentType);
            await content.DisposeAsync();
            return new ObjectResult(ApiResponse.Fail("檔案格式不符預期，請聯絡系統管理員。"))
                { StatusCode = 500 };
        }

        return new FileStreamResult(content, contentType);
    }

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

    // 子路徑版（允許 '/'，用於 blob name 含日期目錄的容器）
    // 仍阻擋 '..' 序列、反斜線、控制字元與空白，避免路徑穿越攻擊
    private static bool IsSafeSubPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(path);
        }
        catch
        {
            return false;
        }

        return !ContainsSubPathTraversal(path) && !ContainsSubPathTraversal(decoded);
    }

    private static bool ContainsTraversal(string value) =>
        value.Contains('/')
        || ContainsSubPathTraversal(value);

    private static bool ContainsSubPathTraversal(string value) =>
        value.Contains('\\')
        || value.Contains("..", StringComparison.Ordinal)
        || value.Contains('\0')
        || value.Any(char.IsControl);
}
