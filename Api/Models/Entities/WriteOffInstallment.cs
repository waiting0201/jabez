namespace Jabez.Api.Models.Entities;

/// <summary>
/// 預支沖銷的分期撥款（公司補撥「本次沖銷造成的超支增額」給員工）。
/// 應撥總額 = <see cref="Jabez.Api.Common.WriteOffRefundCalculator"/> 算出的 RefundDue，
/// 未超支（RefundDue = 0）的沖銷單不會有任何 installment。
/// </summary>
public class WriteOffInstallment : IInstallmentEntity
{
    public int       Id               { get; set; }
    public int       WriteOffRecordId { get; set; }
    public int       InstallmentNo    { get; set; }
    public DateTime  ExpectedDate     { get; set; }
    public DateTime? PaidAt           { get; set; }
    public decimal   Amount           { get; set; }
    public string?   Note             { get; set; }
    public Guid?     PaidByUserId     { get; set; }
    public DateTime  CreatedAt        { get; set; }
    public DateTime  UpdatedAt        { get; set; }

    // Navigation
    public WriteOffRecord WriteOffRecord { get; set; } = null!;
    public User?          PaidBy         { get; set; }
}
