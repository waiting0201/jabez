namespace Jabez.Api.Models.Dtos;

/// <summary>Gemini 使用量（系統自行累計，見 GeminiUsageLog）。From / To 為半開區間 [From, To)。</summary>
public sealed record GeminiUsageDto(
    DateTime From,
    DateTime To,
    int      Calls,
    long     InputTokens,
    long     OutputTokens,
    long     TotalTokens,
    IReadOnlyList<GeminiUsageSourceDto> Sources);

public sealed class GeminiUsageSourceDto
{
    public string Source       { get; set; } = string.Empty;
    public int    Calls        { get; set; }
    public long   InputTokens  { get; set; }
    public long   OutputTokens { get; set; }
    public long   TotalTokens  { get; set; }
}
