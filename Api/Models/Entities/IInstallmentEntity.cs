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

/// <summary>
/// 分期撥款「款別」（現金 / 月結）—— 目前只有預支單的分期實作（2026-11-24）。
/// 預支明細分現金與月結（支票），財務常把兩者拆成不同期撥付；其他 4 種分期沒有月結概念故不實作。
/// <see cref="Services.InstallmentUpsertService"/> 以 <c>is</c> 判斷，未實作者忽略輸入值。
/// </summary>
public interface IInstallmentPaymentMethod
{
    string? PaymentMethod { get; set; }
}
