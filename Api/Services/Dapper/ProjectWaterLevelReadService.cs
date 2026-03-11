using Dapper;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class ProjectWaterLevelReadService(IDbConnection db) : IProjectWaterLevelReadService
{
    /// <summary>
    /// 查詢所有有非 draft 請款紀錄的專案，計算請款金額、已付款金額及佔業務金額百分比。
    /// Percentage 在 C# 端計算，避免 SQL 端除零問題。
    /// </summary>
    public async Task<IEnumerable<ProjectWaterLevelDto>> GetAllAsync()
    {
        const string sql = """
            SELECT p.Id          AS ProjectId,
                   p.Code        AS ProjectCode,
                   p.Status,
                   d.Name        AS DepartmentName,
                   p.BusinessAmount,
                   ISNULL(SUM(pr.TotalAmount), 0)                                              AS PaymentAmount,
                   ISNULL(SUM(CASE WHEN pr.PaidAt IS NOT NULL THEN pr.TotalAmount ELSE 0 END), 0) AS PaidAmount
            FROM   Projects p
            LEFT JOIN Departments      d  ON p.DepartmentId = d.Id
            LEFT JOIN PaymentRequests  pr ON pr.ProjectId   = p.Id
                                         AND pr.ApprovalStatus != 'draft'
            GROUP BY p.Id, p.Code, p.Status, d.Name, p.BusinessAmount
            HAVING SUM(pr.TotalAmount) > 0
            ORDER BY p.Code
            """;

        var rows = await db.QueryAsync<dynamic>(sql);

        return rows.Select(row =>
        {
            decimal paymentAmount = (decimal)row.PaymentAmount;
            decimal paidAmount    = (decimal)row.PaidAmount;
            decimal? businessAmount = row.BusinessAmount is null ? null : (decimal?)row.BusinessAmount;

            // 在 C# 端計算百分比，防止 SQL 端除零例外
            decimal? percentage = (businessAmount.HasValue && businessAmount.Value > 0)
                ? Math.Round(paymentAmount / businessAmount.Value * 100, 1)
                : null;

            return new ProjectWaterLevelDto(
                ProjectId:      (int)row.ProjectId,
                ProjectCode:    (string)row.ProjectCode,
                Status:         (string)row.Status,
                DepartmentName: (string?)row.DepartmentName,
                BusinessAmount: businessAmount,
                PaymentAmount:  paymentAmount,
                PaidAmount:     paidAmount,
                Percentage:     percentage);
        });
    }
}
