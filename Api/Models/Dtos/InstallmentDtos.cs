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
public sealed record UpsertInstallmentsRequest(
    List<InstallmentInput> Installments,
    string?                ApprovalStatus = null);   // 沿用 ApprovalStatus 寫回能力（如：財務節點審核時同步改狀態）

/// <summary>撥款 status 三態</summary>
public enum PaymentInstallmentStatus
{
    Unpaid,
    PartiallyPaid,
    FullyPaid
}
