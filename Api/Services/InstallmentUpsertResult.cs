namespace Jabez.Api.Services;

/// <summary>UpsertInstallments 結果：包含「本次新增的已撥款」清單供通知使用</summary>
public sealed record NewlyPaidInstallment(
    int       InstallmentNo,
    DateTime  PaidAt,
    decimal   Amount,
    int       TotalInstallments);

public sealed record InstallmentUpsertResult(
    DateTime? CacheEstimatedPaymentDate,
    DateTime? CachePaidAt,
    Guid?     CachePaidByUserId,
    List<NewlyPaidInstallment> NewlyPaid);
