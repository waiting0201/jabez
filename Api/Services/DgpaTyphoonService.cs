using Jabez.Api.Common;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Services;

/// <summary>
/// 抓取行政院人事行政總處「天然災害停止上班及上課情形」公告頁。
/// 比照 <see cref="GcisService"/> 的外部服務模式：HttpClient 由 <c>AddHttpClient</c> 注入（BaseAddress / Timeout 在 Program.cs），
/// 任何失敗（逾時、非 2xx、頁面結構改版解析不到）一律記 log 並回 null，**不丟例外**，由排程下一個 tick 重試。
/// 解析規則在純函式 <see cref="DgpaStatusParser"/>。
/// </summary>
public interface IDgpaTyphoonService
{
    Task<DgpaPage?> FetchAsync(CancellationToken ct = default);
}

public sealed class DgpaTyphoonService(HttpClient http, ILogger<DgpaTyphoonService> logger) : IDgpaTyphoonService
{
    private const string PagePath = "typh/daily/nds.html";

    public async Task<DgpaPage?> FetchAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await http.GetAsync(PagePath, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("DGPA 公告頁回應非成功狀態：{Status}", response.StatusCode);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            var html  = System.Text.Encoding.UTF8.GetString(bytes);
            var page  = DgpaStatusParser.ParsePage(html);

            // 頁面結構改版時 counties 會是 0 筆：視為失敗，避免誤判成「全部縣市照常」
            if (page is null || page.Counties.Count == 0)
            {
                logger.LogWarning("DGPA 公告頁解析不到縣市資料（頁面結構可能已改版）");
                return null;
            }
            return page;
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("DGPA 公告頁逾時");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DGPA 公告頁抓取失敗");
            return null;
        }
    }
}
