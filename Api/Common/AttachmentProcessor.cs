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

    /// <summary>單檔上限（前端圖片已壓縮，此為後端安全網）</summary>
    private const long MaxFileBytes = 10 * 1024 * 1024;

    /// <summary>允許的實際 MIME（以 magic byte 偵測，非信任 Content-Type）</summary>
    private static readonly HashSet<string> AllowedTypes =
    [
        "image/png", "image/jpeg", "image/gif", "image/webp",
        "image/heic", "image/avif", "application/pdf",
    ];

    /// <summary>整單附件 multipart JSON 的內部結構</summary>
    public sealed record AttachmentMetadata(string FileName, string? FileUrl, int FileIndex);

    /// <summary>驗證並上傳後解析出的單筆附件（保留既有 URL 或新上傳 URL）</summary>
    public sealed record ResolvedAttachment(string FileName, string? FileUrl);

    /// <summary>
    /// 依 metadata 與上傳檔案清單組裝附件：FileIndex &gt;= 0 者驗證 magic byte / 大小後上傳新檔，
    /// 其餘保留既有 FileUrl。回傳順序與 metadata 一致。
    /// </summary>
    public static async Task<List<ResolvedAttachment>> ResolveAsync(
        AttachmentMetadata[] metas,
        IReadOnlyList<IFormFile> files,
        IBlobStorageService blob)
    {
        var result = new List<ResolvedAttachment>(metas.Length);

        foreach (var m in metas)
        {
            string? fileUrl = m.FileUrl; // 保留既有 URL
            if (m.FileIndex >= 0 && m.FileIndex < files.Count)
            {
                var file = files[m.FileIndex];

                if (file.Length > MaxFileBytes)
                    throw AppException.BadRequest("附件檔案勿超過 10MB。");

                string? actualType;
                using (var peek = file.OpenReadStream())
                    actualType = await FileSignatureValidator.DetectAsync(peek);

                if (actualType is null || !AllowedTypes.Contains(actualType))
                    throw AppException.BadRequest("附件僅支援 PNG、JPEG、GIF、WebP、HEIC 圖片或 PDF 格式。");

                var ext      = Path.GetExtension(file.FileName);
                var blobName = $"{Clock.Now:yyyy/MM}/{Guid.NewGuid()}{ext}";
                using (var stream = file.OpenReadStream())
                    fileUrl = await blob.UploadAsync(ContainerName, blobName, stream, actualType);
            }

            result.Add(new ResolvedAttachment(m.FileName, fileUrl));
        }

        return result;
    }
}
