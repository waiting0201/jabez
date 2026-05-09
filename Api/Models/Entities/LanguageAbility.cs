using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>語言能力（1 User : N LanguageAbility）</summary>
public class LanguageAbility
{
    public Guid   Id        { get; set; }
    public Guid   UserId    { get; set; }
    public string Language  { get; set; } = string.Empty;   // 語言名稱（如：英文、日文）
    public string Listening { get; set; } = string.Empty;   // good / fair
    public string Speaking  { get; set; } = string.Empty;   // good / fair
    public string Reading   { get; set; } = string.Empty;   // good / fair
    public string Writing   { get; set; } = string.Empty;   // good / fair

    public DateTime CreatedAt { get; set; } = Clock.Now;
    public DateTime UpdatedAt { get; set; } = Clock.Now;
}
