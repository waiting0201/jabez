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
    string?   PaidBySignatureUrl = null,
    string?   PaymentMethod      = null);   // 款別 cash / check / null（僅預支單分期有值）

/// <summary>分期撥款輸入（upsert 用）</summary>
public sealed record InstallmentInput(
    int?      Id,
    int       InstallmentNo,
    DateTime  ExpectedDate,
    DateTime? PaidAt,
    decimal   Amount,
    string?   Note,
    string?   PaymentMethod = null);   // 款別 cash / check / null；僅預支單分期寫入，其他類型忽略

/// <summary>upsert 分期撥款請求（4 種申請類型共用）</summary>
/// <remarks>
/// 2026-10 安全修正：移除 ApprovalStatus 欄位。撥款明細端點只管撥款資料，不得藉此改單據簽核狀態
/// （曾可讓財務體系人員把請款單由任何狀態直接改成 approved / draft，繞過簽核）。
/// </remarks>
public sealed record UpsertInstallmentsRequest(
    List<InstallmentInput> Installments);

/// <summary>分期撥款款別（預支單專用，2026-11-24）</summary>
public static class InstallmentPaymentMethods
{
    public const string Cash  = "cash";
    public const string Check = "check";

    /// <summary>空白 → null（未分類）；cash / check 原樣；其他值丟 400</summary>
    public static string? Normalize(string? value, int installmentNo)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim().ToLowerInvariant();
        if (v is Cash or Check) return v;
        throw Jabez.Api.Common.AppException.BadRequest($"第 {installmentNo} 期款別不正確（只接受現金 / 月結）。");
    }
}

/// <summary>撥款 status 三態</summary>
public enum PaymentInstallmentStatus
{
    Unpaid,
    PartiallyPaid,
    FullyPaid
}
