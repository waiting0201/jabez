namespace Jabez.Api.Models.Entities;

/// <summary>
/// Gemini API 呼叫用量紀錄（2026-11）：發票 / 報價單 OCR 每次**成功**呼叫 Gemini 留一列，
/// token 數取自 Gemini 回應的 <c>usageMetadata</c>。Gemini 的 API key 查不到用量（帳單在 Google Cloud Billing），
/// 故由系統自行累計，供打卡頁「Gemini 使用量」卡片顯示；只統計本系統的呼叫、自上線日起算。
/// 寫入見 <see cref="Jabez.Api.Services.GeminiUsageRecorder"/>。
/// </summary>
public class GeminiUsageLog
{
    public long     Id           { get; set; }
    /// <summary>invoice-ocr / quote-ocr（見 GeminiUsageSources）</summary>
    public string   Source       { get; set; } = string.Empty;
    public string   Model        { get; set; } = string.Empty;
    /// <summary>usageMetadata.promptTokenCount</summary>
    public int      InputTokens  { get; set; }
    /// <summary>candidatesTokenCount + thoughtsTokenCount（兩者皆以輸出計費）</summary>
    public int      OutputTokens { get; set; }
    public int      TotalTokens  { get; set; }
    public DateTime CreatedAt    { get; set; }
}
