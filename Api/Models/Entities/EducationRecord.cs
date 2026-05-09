using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>學歷紀錄（1 User : N EducationRecord）</summary>
public class EducationRecord
{
    public Guid   Id         { get; set; }
    public Guid   UserId     { get; set; }
    public string School     { get; set; } = string.Empty;   // 學校名稱
    public string? Department { get; set; }                   // 科系
    public string  Degree    { get; set; } = string.Empty;   // graduated / incomplete
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate   { get; set; }
    public int     Order     { get; set; } = 1;              // 顯示排序（1-3）

    public DateTime CreatedAt { get; set; } = Clock.Now;
    public DateTime UpdatedAt { get; set; } = Clock.Now;
}
