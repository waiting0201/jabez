using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>
/// 勞健保級距資料（投保薪資級距與員工負擔保費）
/// </summary>
public class InsuranceBracket
{
    public int      Id                      { get; set; }

    /// <summary>投保級距（薪資金額）</summary>
    public decimal  SalaryBracket           { get; set; }

    /// <summary>員工負擔勞保費</summary>
    public decimal  LaborInsuranceEmployee  { get; set; }

    /// <summary>員工負擔健保費</summary>
    public decimal  HealthInsuranceEmployee { get; set; }

    public DateTime CreatedAt               { get; set; } = Clock.Now;
}
