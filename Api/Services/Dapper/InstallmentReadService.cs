using Dapper;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

/// <summary>
/// 共用分期撥款查詢服務 — 給 4 種申請類型（請款/預支/出差/出差請款）的 ReadService 使用。
/// 一次取出指定父表多個 id 的所有 installments + 撥款人簽名章（JOIN Users.SignatureUrl）。
/// 同時計算三態 status（Unpaid / PartiallyPaid / FullyPaid）。
/// </summary>
public interface IInstallmentReadService
{
    /// <summary>對應 4 種父表，回傳 {parentId -> installments list}</summary>
    Task<Dictionary<int, List<InstallmentDto>>> GetByParentIdsAsync(InstallmentParentTable table, IEnumerable<int> parentIds);

    /// <summary>計算三態 status（給單筆 task detail 用）</summary>
    string ComputeStatus(IReadOnlyList<InstallmentDto> installments);
}

public enum InstallmentParentTable
{
    PaymentRequest,
    AdvanceRequest,
    TravelRequest,
    TravelPaymentRequest,
}

public sealed class InstallmentReadService(IDbConnection db) : IInstallmentReadService
{
    public async Task<Dictionary<int, List<InstallmentDto>>> GetByParentIdsAsync(
        InstallmentParentTable table, IEnumerable<int> parentIds)
    {
        var ids = parentIds.Distinct().ToList();
        var result = new Dictionary<int, List<InstallmentDto>>();
        if (ids.Count == 0) return result;

        var (tableName, fkCol) = table switch
        {
            InstallmentParentTable.PaymentRequest      => ("PaymentRequestInstallments",      "PaymentRequestId"),
            InstallmentParentTable.AdvanceRequest      => ("AdvanceRequestInstallments",      "AdvanceRequestId"),
            InstallmentParentTable.TravelRequest       => ("TravelRequestInstallments",       "TravelRequestId"),
            InstallmentParentTable.TravelPaymentRequest => ("TravelPaymentRequestInstallments", "TravelPaymentRequestId"),
            _ => throw new ArgumentOutOfRangeException(nameof(table))
        };

        var sql = $"""
            SELECT  i.Id,
                    i.{fkCol}              AS ParentId,
                    i.InstallmentNo,
                    i.ExpectedDate,
                    i.PaidAt,
                    i.Amount,
                    i.Note,
                    i.PaidByUserId,
                    u.Name                 AS PaidByName,
                    u.SignatureUrl         AS PaidBySignatureUrl
            FROM {tableName} i
            LEFT JOIN Users u ON u.Id = i.PaidByUserId
            WHERE i.{fkCol} IN @Ids
            ORDER BY i.{fkCol}, i.InstallmentNo
            """;

        var rows = await db.QueryAsync(sql, new { Ids = ids });
        foreach (var row in rows)
        {
            var parentId = (int)row.ParentId;
            if (!result.TryGetValue(parentId, out var list))
            {
                list = new List<InstallmentDto>();
                result[parentId] = list;
            }
            list.Add(new InstallmentDto(
                (int)row.Id,
                (int)row.InstallmentNo,
                (DateTime)row.ExpectedDate,
                (DateTime?)row.PaidAt,
                (decimal)row.Amount,
                (string?)row.Note,
                (Guid?)row.PaidByUserId,
                (string?)row.PaidByName,
                (string?)row.PaidBySignatureUrl));
        }

        return result;
    }

    public string ComputeStatus(IReadOnlyList<InstallmentDto> installments)
    {
        if (installments.Count == 0) return nameof(PaymentInstallmentStatus.Unpaid);
        var paidCount = installments.Count(i => i.PaidAt.HasValue);
        if (paidCount == 0) return nameof(PaymentInstallmentStatus.Unpaid);
        if (paidCount < installments.Count) return nameof(PaymentInstallmentStatus.PartiallyPaid);
        return nameof(PaymentInstallmentStatus.FullyPaid);
    }
}
