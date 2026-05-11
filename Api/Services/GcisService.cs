using Jabez.Api.Models.Dtos;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Jabez.Api.Services;

/// <summary>
/// GCIS 商工登記公示資料查詢 API 包裝。
/// 文件：https://data.gcis.nat.gov.tw/od/data/api/5F64D864-61CB-4D0D-8AD9-492047CC1EA6
/// 公司登記 endpoint 回傳欄位包含 Company_Name / Company_Location / Responsible_Name / Business_Accounting_NO。
/// 免 API Key、免註冊，由 IHttpClientFactory 注入 HttpClient（已設定 BaseAddress 與 Timeout）。
/// </summary>
public sealed class GcisService(HttpClient http, ILogger<GcisService> logger) : IGcisService
{
    // 公司登記資料查詢 API 的 dataset id（GCIS Open Data）
    private const string DatasetId = "5F64D864-61CB-4D0D-8AD9-492047CC1EA6";

    public async Task<VendorTaxIdLookupResponse?> LookupByTaxIdAsync(string taxId, CancellationToken ct = default)
    {
        // GCIS OData filter：Business_Accounting_NO eq '<taxId>'（注意：值需以單引號包覆）
        var url = $"od/data/api/{DatasetId}?$format=json&$filter=Business_Accounting_NO eq '{taxId}'&$skip=0&$top=1";

        try
        {
            using var response = await http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("GCIS API non-success status: {Status} for taxId {TaxId}", response.StatusCode, taxId);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                return null;

            var first = doc.RootElement[0];
            var name  = GetString(first, "Company_Name");
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return new VendorTaxIdLookupResponse(
                TaxId:         taxId,
                Name:          name,
                Address:       NullIfEmpty(GetString(first, "Company_Location")),
                ContactPerson: NullIfEmpty(GetString(first, "Responsible_Name")));
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("GCIS API timeout for taxId {TaxId}", taxId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GCIS API call failed for taxId {TaxId}", taxId);
            return null;
        }
    }

    private static string GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString() ?? string.Empty
            : string.Empty;

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
