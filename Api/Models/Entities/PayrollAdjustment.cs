using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>每位員工每月的薪資調整項目（其他扣項、備注）</summary>
public class PayrollAdjustment
{
    public int       Id                   { get; set; }
    public Guid      EmployeeId           { get; set; }
    public int       Year                 { get; set; }
    public int       Month                { get; set; }
    public decimal   OtherAddition        { get; set; }      // 其他加項金額
    public string?   OtherAdditionNote    { get; set; }      // 其他加項說明
    public decimal   OtherDeduction       { get; set; }      // 其他扣項金額
    public string?   OtherDeductionNote   { get; set; }      // 其他扣項說明
    public string?   Note                 { get; set; }      // 備注
    public DateTime  CreatedAt            { get; set; } = Clock.Now;
    public DateTime  UpdatedAt            { get; set; } = Clock.Now;

    // Navigation
    public User? Employee { get; set; }
}
