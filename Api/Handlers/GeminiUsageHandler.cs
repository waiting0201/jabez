using Jabez.Api.Common;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>Gemini 使用量（打卡頁卡片，2026-11）。權限沿用 line-quota:read，與 LINE 推播用量卡片同一批人看得到。</summary>
public sealed class GeminiUsageHandler(IGeminiUsageReadService readService)
{
    /// <summary>GET /gemini/usage — 本月（台北時間 1 號起）Gemini 呼叫次數與 token 合計。</summary>
    public async Task<IActionResult> GetUsageAsync(HttpRequest req)
    {
        var now  = Clock.Now;
        var from = new DateTime(now.Year, now.Month, 1);
        var usage = await readService.GetSummaryAsync(from, from.AddMonths(1));
        return new OkObjectResult(ApiResponse.Ok(usage));
    }
}
