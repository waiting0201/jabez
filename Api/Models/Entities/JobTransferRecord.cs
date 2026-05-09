using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>
/// 職務調整歷史（1 User : N JobTransferRecord）。
/// FromDepartment / ToDepartment / FromJobTitle / ToJobTitle 為字串 snapshot，不 FK，
/// 避免組織重整後歷史紀錄失真。
/// </summary>
public class JobTransferRecord
{
    public Guid     Id              { get; set; }
    public Guid     UserId          { get; set; }
    public DateTime EffectiveDate   { get; set; }          // 生效日期
    public string?  FromDepartment  { get; set; }          // 調整前部門（snapshot）
    public string?  ToDepartment    { get; set; }          // 調整後部門（snapshot）
    public string?  FromJobTitle    { get; set; }          // 調整前職稱（snapshot）
    public string?  ToJobTitle      { get; set; }          // 調整後職稱（snapshot）

    public DateTime CreatedAt { get; set; } = Clock.Now;
    public DateTime UpdatedAt { get; set; } = Clock.Now;
}
