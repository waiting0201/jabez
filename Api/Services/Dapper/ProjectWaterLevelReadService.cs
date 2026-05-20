using Dapper;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class ProjectWaterLevelReadService(IDbConnection db) : IProjectWaterLevelReadService
{
    /// <summary>
    /// 查詢所有有非 draft 請款紀錄的專案，計算請款金額、已付款金額及佔業務執行金額百分比。
    /// Percentage 在 C# 端計算，避免 SQL 端除零問題。
    /// 套用 CLAUDE.md「部門可見性規則」：可見範圍由 IProjectAccessResolver 決定（Superadmin / CanSeeAll → SeeAll；其他依 CanViewSiblings / CanViewDescendants 旗標聯集）。
    /// </summary>
    public async Task<IEnumerable<ProjectWaterLevelDto>> GetAllAsync(ProjectAccessScope scope, int? year = null, string? status = null)
    {
        var param = new DynamicParameters();
        var conditions = new List<string>();

        if (!scope.SeeAll)
        {
            if (scope.AllowedDepartmentIds.Count == 0)
                conditions.Add("1 = 0");
            else
            {
                conditions.Add("p.DepartmentId IN @AllowedDeptIds");
                param.Add("AllowedDeptIds", scope.AllowedDepartmentIds);
            }
        }

        if (year.HasValue)
        {
            conditions.Add("YEAR(p.StartDate) = @Year");
            param.Add("Year", year.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("p.Status = @Status");
            param.Add("Status", status);
        }

        var scopeClause = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : "";

        // PaidAmount 從 installments 子表累計：每個 PaymentRequest 取「已撥的 installments 金額加總」
        // 與舊「PaidAt IS NOT NULL THEN TotalAmount」相比，分期撥款情境下更精準（部分撥款也計入實際金額）
        // 使用 OUTER APPLY 預算 per-PR 已撥金額，外層 SUM 才能正確聚合（SQL Server 不允許 SUM 直接包 SUM 子查詢）
        var sql = $"""
            SELECT p.Id          AS ProjectId,
                   p.Code        AS ProjectCode,
                   p.Name        AS ProjectName,
                   p.Status,
                   d.Name        AS DepartmentName,
                   p.ContractAmount,
                   p.BusinessAmount,
                   p.RemainingAmount,
                   ISNULL(SUM(pr.TotalAmount), 0) AS PaymentAmount,
                   ISNULL(SUM(paid.Amount), 0)    AS PaidAmount
            FROM   Projects p
            LEFT JOIN Departments      d  ON p.DepartmentId = d.Id
            LEFT JOIN PaymentRequests  pr ON pr.ProjectId   = p.Id
                                         AND pr.ApprovalStatus != 'draft'
            OUTER APPLY (
              SELECT SUM(i.Amount) AS Amount
              FROM PaymentRequestInstallments i
              WHERE i.PaymentRequestId = pr.Id AND i.PaidAt IS NOT NULL
            ) paid
            {scopeClause}
            GROUP BY p.Id, p.Code, p.Name, p.Status, d.Name, p.ContractAmount, p.BusinessAmount, p.RemainingAmount
            HAVING SUM(pr.TotalAmount) > 0
            ORDER BY p.Code
            """;

        var rows = await db.QueryAsync<dynamic>(sql, param);

        return rows.Select(row =>
        {
            decimal paymentAmount = (decimal)row.PaymentAmount;
            decimal paidAmount    = (decimal)row.PaidAmount;
            decimal? contractAmount  = row.ContractAmount  is null ? null : (decimal?)row.ContractAmount;
            decimal? businessAmount  = row.BusinessAmount  is null ? null : (decimal?)row.BusinessAmount;
            decimal? remainingAmount = row.RemainingAmount is null ? null : (decimal?)row.RemainingAmount;

            // 系統導入前已使用金額；資料異常（剩餘 > 契約）一律視為 0 避免負數
            decimal preImportUsed = (contractAmount.HasValue && remainingAmount.HasValue
                                     && contractAmount.Value >= remainingAmount.Value)
                ? contractAmount.Value - remainingAmount.Value
                : 0m;

            // 在 C# 端計算百分比，防止 SQL 端除零例外
            decimal? percentage = (businessAmount.HasValue && businessAmount.Value > 0)
                ? Math.Round(paymentAmount / businessAmount.Value * 100, 1)
                : null;

            decimal? totalPercentage = (contractAmount.HasValue && contractAmount.Value > 0)
                ? Math.Round((paymentAmount + preImportUsed) / contractAmount.Value * 100, 1)
                : null;

            return new ProjectWaterLevelDto(
                ProjectId:           (int)row.ProjectId,
                ProjectCode:         (string)row.ProjectCode,
                ProjectName:         (string)row.ProjectName,
                Status:              (string)row.Status,
                DepartmentName:      (string?)row.DepartmentName,
                ContractAmount:      contractAmount,
                BusinessAmount:      businessAmount,
                RemainingAmount:     remainingAmount,
                PaymentAmount:       paymentAmount,
                PaidAmount:          paidAmount,
                PreImportUsedAmount: preImportUsed,
                Percentage:          percentage,
                TotalPercentage:     totalPercentage);
        });
    }
}
