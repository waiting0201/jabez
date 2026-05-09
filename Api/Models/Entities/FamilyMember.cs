using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>家庭成員（1 User : N FamilyMember）</summary>
public class FamilyMember
{
    public Guid   Id           { get; set; }
    public Guid   UserId       { get; set; }
    public string Name         { get; set; } = string.Empty;   // 姓名
    public string Relationship { get; set; } = string.Empty;   // 關係（配偶/父/母/子/女...）
    public int?   Age          { get; set; }                   // 年齡
    public string? Occupation  { get; set; }                   // 職業

    public DateTime CreatedAt { get; set; } = Clock.Now;
    public DateTime UpdatedAt { get; set; } = Clock.Now;
}
