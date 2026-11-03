using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IGeminiUsageReadService
{
    /// <summary>[from, to) 區間內的 Gemini 呼叫次數與 token 合計（含依來源分組）。</summary>
    Task<GeminiUsageDto> GetSummaryAsync(DateTime from, DateTime to);
}
