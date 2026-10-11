namespace Jabez.Api.Models.Dtos;

/// <summary>分期撥款明細 DTO（回應用）</summary>
public sealed record InstallmentDto(
    int       Id,
    int       InstallmentNo,
    DateTime  ExpectedDate,
    DateTime? PaidAt,
    decimal   Amount,
    string?   Note,
    Guid?     PaidByUserId,
    string?   PaidByName         = null,
    string?   PaidBySignatureUrl = null);

/// <summary>分期撥款輸入（upsert 用）</summary>
public sealed record InstallmentInput(
    int?      Id,
    int       InstallmentNo,
    DateTime  ExpectedDate,
    DateTime? PaidAt,
    decimal   Amount,
    string?   Note);

/// <summary>upsert 分期撥款請求（4 種申請類型共用）</summary>
/// <remarks>
/// 2026-10 安全修正：移除 ApprovalStatus 欄位。撥款明細端點只管撥款資料，不得藉此改單據簽核狀態
/// （曾可讓財務體系人員把請款單由任何狀態直接改成 approved / draft，繞過簽核）。
/// </remarks>
public sealed record UpsertInstallmentsRequest(
    List<InstallmentInput> Installments);

/// <summary>撥款 status 三態</summary>
public enum PaymentInstallmentStatus
{
    Unpaid,
    PartiallyPaid,
    FullyPaid
}
