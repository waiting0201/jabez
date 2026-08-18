using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>
/// 薪資調整歷史（1 User : N SalaryAdjustmentRecord）。
/// 新增最新有效紀錄後自動同步 User.BaseSalary。
/// </summary>
public class SalaryAdjustmentRecord
{
    public Guid     Id                   { get; set; }
    public Guid     UserId               { get; set; }
    public DateTime EffectiveDate        { get; set; }        // 生效日期
    public decimal  BaseSalary           { get; set; }        // 底薪
    // 2026-08 移除職務加給 / 主管加給 / 外派加給；DB 欄位保留作歷史封存，未產 DROP migration。
    public decimal? OtherAllowance       { get; set; }        // 其他加給
    public decimal? AdjustmentDifference { get; set; }        // 調整差額
    public decimal? MealAllowance        { get; set; }        // 伙食費
    public decimal  TotalAmount          { get; set; }        // 合計金額
    public string?  Notes                { get; set; }        // 備註

    public DateTime CreatedAt { get; set; } = Clock.Now;
    public DateTime UpdatedAt { get; set; } = Clock.Now;
}
