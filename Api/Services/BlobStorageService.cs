using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace Jabez.Api.Services;

public sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;

    public BlobStorageService(IConfiguration cfg)
    {
        var connStr = cfg["BlobStorageConnection"]
            ?? throw new InvalidOperationException("BlobStorageConnection is required.");
        _client = new BlobServiceClient(connStr);
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

    public string? ExtractBlobName(string? blobUrl, string containerName)
    {
        if (string.IsNullOrEmpty(blobUrl)) return null;
        var marker = $"/{containerName}/";
        var idx = blobUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? blobUrl[(idx + marker.Length)..] : null;
    }
}
