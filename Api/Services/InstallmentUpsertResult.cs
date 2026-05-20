namespace Jabez.Api.Services;

/// <summary>UpsertInstallments 結果：本次新增的已撥款清單供通知使用</summary>
public sealed record NewlyPaidInstallment(
    int       InstallmentNo,
    DateTime  PaidAt,
    decimal   Amount,
    int       TotalInstallments);
