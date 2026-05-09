using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>獎懲紀錄（1 User : N RewardPunishmentRecord）</summary>
public class RewardPunishmentRecord
{
    public Guid     Id            { get; set; }
    public Guid     UserId        { get; set; }
    public DateTime EffectiveDate { get; set; }            // 獎懲生效日
    public string   Type         { get; set; } = string.Empty;   // reward / punishment
    public string?  Category     { get; set; }             // 類別（如：嘉獎 / 申誡）
    public int      Count        { get; set; } = 1;        // 次數
    public string?  Reason       { get; set; }             // 事由（nvarchar max）

    public DateTime CreatedAt { get; set; } = Clock.Now;
    public DateTime UpdatedAt { get; set; } = Clock.Now;
}
