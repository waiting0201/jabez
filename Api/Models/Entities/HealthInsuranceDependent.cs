using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>
/// 健保眷屬（1 User : N HealthInsuranceDependent）。
/// 健保費計算：baseHealth × (1 + min(眷屬數, 3))。
/// </summary>
public class HealthInsuranceDependent
{
    public Guid     Id           { get; set; }
    public Guid     UserId       { get; set; }
    public string   Name         { get; set; } = string.Empty;   // 眷屬姓名
    public string   Relationship { get; set; } = string.Empty;   // 關係（配偶/父/母/子/女...）
    public string?  IdNumber     { get; set; }                   // 身分證號
    public DateTime? BirthDate   { get; set; }                   // 出生日期

    public DateTime CreatedAt { get; set; } = Clock.Now;
    public DateTime UpdatedAt { get; set; } = Clock.Now;
}
