using Jabez.Api.Services;
using Microsoft.AspNetCore.Http;

namespace Jabez.Api.Common;

/// <summary>
/// 整單批次附件（照片 / PDF）的共用處理：multipart metadata 解析模型、magic-byte 驗證、
/// 上傳至 Blob Storage。請款（一般請款）與預支沖銷共用同一容器與同一驗證規則。
/// </summary>
public static class AttachmentProcessor
{
    /// <summary>整單附件的 Blob 容器名稱（請款 + 沖銷共用）</summary>
    public const string ContainerName = "request-attachments";

    /// <summary>單檔上限（前端圖片已壓縮，此為後端安全網；明細檔與整單附件共用）</summary>
    public const long MaxFileBytes = 10 * 1024 * 1024;

    /// <summary>允許的實際 MIME（以 magic byte 偵測，非信任 Content-Type）</summary>
    private static readonly HashSet<string> AllowedTypes =
    [
        "image/png", "image/jpeg", "image/gif", "image/webp",
        "image/heic", "image/avif", "application/pdf",
    ];

    /// <summary>實際 MIME → 副檔名（副檔名由偵測結果決定，不信任客戶端檔名）</summary>
    private static string ExtensionFor(string mime) => mime switch
    {
        "image/png"       => ".png",
        "image/jpeg"      => ".jpg",
        "image/gif"       => ".gif",
        "image/webp"      => ".webp",
        "image/heic"      => ".heic",
        "image/avif"      => ".avif",
        "application/pdf" => ".pdf",
        _                 => ".bin",
    };

    /// <summary>新建單據用：沒有任何既有 URL 可保留</summary>
    public static readonly IReadOnlySet<string> NoOwnedUrls = new HashSet<string>();

    /// <summary>
    /// 明細 / 附件「保留既有 URL」的白名單比對（V4）：只接受這張單目前 DB 內本來就有的 FileUrl，
    /// 其餘（javascript:、外部網址、他人 blob）一律視為無檔（回 null）。
    /// 新建單據傳入空集合 → 客戶端送來的 FileUrl 一律忽略。
    /// </summary>
    public static string? KeepExistingUrl(string? clientUrl, IReadOnlySet<string> ownedUrls)
        => !string.IsNullOrEmpty(clientUrl) && ownedUrls.Contains(clientUrl) ? clientUrl : null;

    /// <summary>
    /// 明細檔案上傳（V18）：大小上限 + magic-byte 白名單 + 以偵測型別決定副檔名與 Content-Type。
    /// 回傳新 blob 的 URL。
    /// </summary>
    public static async Task<string> UploadItemFileAsync(IFormFile file, IBlobStorageService blob, string containerName)
    {
        if (file.Length > MaxFileBytes)
            throw AppException.BadRequest("檔案勿超過 10MB。");

        string? actualType;
        using (var peek = file.OpenReadStream())
            actualType = await FileSignatureValidator.DetectAsync(peek);

        if (actualType is null || !AllowedTypes.Contains(actualType))
            throw AppException.BadRequest("檔案僅支援 PNG、JPEG、GIF、WebP、HEIC 圖片或 PDF 格式。");

        var blobName = $"{Clock.Now:yyyy/MM}/{Guid.NewGuid()}{ExtensionFor(actualType)}";
        using var stream = file.OpenReadStream();
        return await blob.UploadAsync(containerName, blobName, stream, actualType);
    }

    /// <summary>整單附件 multipart JSON 的內部結構</summary>
    public sealed record AttachmentMetadata(string FileName, string? FileUrl, int FileIndex);

    /// <summary>驗證並上傳後解析出的單筆附件（保留既有 URL 或新上傳 URL）</summary>
    public sealed record ResolvedAttachment(string FileName, string? FileUrl);

    /// <summary>
    /// 依 metadata 與上傳檔案清單組裝附件：FileIndex &gt;= 0 者驗證 magic byte / 大小後上傳新檔，
    /// 其餘僅在 ownedUrls（本單 DB 既有 FileUrl）內才保留。回傳順序與 metadata 一致。
    /// </summary>
    public static async Task<List<ResolvedAttachment>> ResolveAsync(
        AttachmentMetadata[] metas,
        IReadOnlyList<IFormFile> files,
        IBlobStorageService blob,
        IReadOnlySet<string> ownedUrls)
    {
        var result = new List<ResolvedAttachment>(metas.Length);

        foreach (var m in metas)
        {
            string? fileUrl = KeepExistingUrl(m.FileUrl, ownedUrls); // 保留既有 URL：僅限本單 DB 既有值
            if (m.FileIndex >= 0 && m.FileIndex < files.Count)
            {
                fileUrl = await UploadItemFileAsync(files[m.FileIndex], blob, ContainerName);
            }

            result.Add(new ResolvedAttachment(m.FileName, fileUrl));
        }

        return result;
    }
}
