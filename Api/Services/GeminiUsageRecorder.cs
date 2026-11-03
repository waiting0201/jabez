using System.Text.Json.Serialization;
using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Services;

/// <summary>Gemini 回應的 usageMetadata（OCR Handler 反序列化時共用）。</summary>
public sealed class GeminiUsageMetadata
{
    [JsonPropertyName("promptTokenCount")]
    public int PromptTokenCount { get; set; }

    [JsonPropertyName("candidatesTokenCount")]
    public int CandidatesTokenCount { get; set; }

    [JsonPropertyName("thoughtsTokenCount")]
    public int ThoughtsTokenCount { get; set; }

    [JsonPropertyName("totalTokenCount")]
    public int TotalTokenCount { get; set; }
}

public static class GeminiUsageSources
{
    public const string InvoiceOcr = "invoice-ocr";
    public const string QuoteOcr   = "quote-ocr";
}

/// <summary>
/// 寫入 <see cref="GeminiUsageLog"/>。記錄失敗只寫 log、不往外丟 —— 用量統計不可以讓 OCR 本身失敗。
/// </summary>
public sealed class GeminiUsageRecorder(AppDbContext db, ILogger<GeminiUsageRecorder> logger)
{
    public async Task RecordAsync(string source, string model, GeminiUsageMetadata? usage)
    {
        try
        {
            var input  = usage?.PromptTokenCount ?? 0;
            var output = (usage?.CandidatesTokenCount ?? 0) + (usage?.ThoughtsTokenCount ?? 0);
            db.GeminiUsageLogs.Add(new GeminiUsageLog
            {
                Source       = source,
                Model        = model.Length > 64 ? model[..64] : model,
                InputTokens  = input,
                OutputTokens = output,
                TotalTokens  = usage?.TotalTokenCount is > 0 ? usage.TotalTokenCount : input + output,
                CreatedAt    = Clock.Now,
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GeminiUsageRecorder failed: source={Source}", source);
        }
    }
}
