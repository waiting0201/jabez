using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Services;

public sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IConfiguration cfg, ILogger<BlobStorageService> logger)
    {
        var connStr = cfg["BlobStorageConnection"]
            ?? throw new InvalidOperationException("BlobStorageConnection is required.");
        _client = new BlobServiceClient(connStr);
        _logger = logger;
    }

    public async Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType)
    {
        var container = _client.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobClient = container.GetBlobClient(blobName);
        await blobClient.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        });

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string containerName, string blobName)
    {
        var container = _client.GetBlobContainerClient(containerName);
        await container.GetBlobClient(blobName).DeleteIfExistsAsync();
    }

    public async Task<(Stream Content, string ContentType)?> DownloadAsync(string containerName, string blobName)
    {
        var container = _client.GetBlobContainerClient(containerName);

        // 找不到一律降為 null（→ 上層回傳 404 而非 500）；
        // 其他狀態（403 auth / 5xx 服務錯誤）以 LogError 記錄並重拋（500 with alert）。
        try
        {
            if (!await container.ExistsAsync())
                return null;

            var blobClient = container.GetBlobClient(blobName);
            if (!await blobClient.ExistsAsync())
                return null;

            var download = await blobClient.DownloadAsync();
            // ContentType 可能為空，預設回傳 application/octet-stream
            var contentType = download.Value.ContentType ?? "application/octet-stream";
            return (download.Value.Content, contentType);
        }
        catch (RequestFailedException ex) when (ex.Status == 404 || ex.ErrorCode is "ContainerNotFound" or "BlobNotFound")
        {
            return null;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex,
                "Blob 取得失敗（非 404）：container={Container} blob={Blob} status={Status} code={Code}",
                containerName, blobName, ex.Status, ex.ErrorCode);
            throw;
        }
    }

    public string? ExtractBlobName(string? blobUrl, string containerName)
    {
        if (string.IsNullOrEmpty(blobUrl)) return null;
        var marker = $"/{containerName}/";
        var idx = blobUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? blobUrl[(idx + marker.Length)..] : null;
    }
}
