using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>工作經歷紀錄（1 User : N EmploymentHistoryRecord）</summary>
public class EmploymentHistoryRecord
{
    public Guid   Id           { get; set; }
    public Guid   UserId       { get; set; }
    public string Organization { get; set; } = string.Empty;   // 服務機構/公司
    public string  JobTitle    { get; set; } = string.Empty;   // 職稱（snapshot，不 FK）
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate   { get; set; }
    public int     Order       { get; set; } = 1;              // 顯示排序

    public DateTime CreatedAt { get; set; } = Clock.Now;
    public DateTime UpdatedAt { get; set; } = Clock.Now;
}
