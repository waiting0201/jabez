namespace Jabez.Api.Models.Entities;

/// <summary>
/// 5 種分期撥款子表（PaymentRequestInstallment / AdvanceRequestInstallment /
/// TravelRequestInstallment / TravelPaymentRequestInstallment / WriteOffInstallment）共用的可寫欄位介面，
/// 供 <see cref="Jabez.Api.Services.InstallmentUpsertService"/> 以泛型方式統一 upsert。
/// </summary>
public interface IInstallmentEntity
{
    int       Id            { get; set; }
    int       InstallmentNo { get; set; }
    DateTime  ExpectedDate  { get; set; }
    DateTime? PaidAt        { get; set; }
    decimal   Amount        { get; set; }
    string?   Note          { get; set; }
    Guid?     PaidByUserId  { get; set; }
    DateTime  CreatedAt     { get; set; }
    DateTime  UpdatedAt     { get; set; }
}
