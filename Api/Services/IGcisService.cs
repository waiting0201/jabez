using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services;

/// <summary>
/// 政府開放資料平臺（GCIS）商工登記公示資料查詢服務。
/// 用於以統編查詢公司名稱 / 地址 / 負責人。
/// </summary>
public interface IGcisService
{
    /// <summary>
    /// 以統一編號查詢公司登記資料。
    /// 找不到、API 失敗或 timeout 一律回傳 null（由呼叫端轉成 404）。
    /// </summary>
    Task<VendorTaxIdLookupResponse?> LookupByTaxIdAsync(string taxId, CancellationToken ct = default);
}
