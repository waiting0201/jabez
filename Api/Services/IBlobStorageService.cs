namespace Jabez.Api.Services;

public interface IBlobStorageService
{
    /// <summary>上傳檔案至指定容器，回傳完整 Blob URL。</summary>
    Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType);

    /// <summary>刪除指定容器中的 Blob。</summary>
    Task DeleteAsync(string containerName, string blobName);

    /// <summary>從完整 Blob URL 萃取 blob 名稱（容器後的路徑）。</summary>
    string? ExtractBlobName(string? blobUrl, string containerName);

    /// <summary>從指定容器下載 Blob，回傳串流與 Content-Type。找不到時回傳 null。</summary>
    Task<(Stream Content, string ContentType)?> DownloadAsync(string containerName, string blobName);
}
