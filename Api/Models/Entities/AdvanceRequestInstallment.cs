namespace Jabez.Api.Models.Entities;

public class AdvanceRequestInstallment : IInstallmentEntity, IInstallmentPaymentMethod
{
    public int       Id               { get; set; }
    public int       AdvanceRequestId { get; set; }
    public int       InstallmentNo    { get; set; }
    public DateTime  ExpectedDate     { get; set; }
    public DateTime? PaidAt           { get; set; }
    public decimal   Amount           { get; set; }
    public string?   Note             { get; set; }
    /// <summary>款別：cash 現金 / check 月結；null ＝ 未分類（2026-11-24 前的舊資料，由財務補標）</summary>
    public string?   PaymentMethod    { get; set; }
    public Guid?     PaidByUserId     { get; set; }
    public DateTime  CreatedAt        { get; set; }
    public DateTime  UpdatedAt        { get; set; }

    // Navigation
    public AdvanceRequest AdvanceRequest { get; set; } = null!;
    public User?          PaidBy         { get; set; }
}
