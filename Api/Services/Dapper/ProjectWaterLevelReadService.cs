using Dapper;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class ProjectWaterLevelReadService(IDbConnection db) : IProjectWaterLevelReadService
{
    /// <summary>
    /// 查詢可見範圍內的**全部**專案，計算動支金額佔業務執行金額 / 契約金額百分比。
    /// （2026-07 起不再過濾 DisbursedAmount = 0：尚未撥款的專案也要列出，水位顯示 0%，
    ///   否則新匯入 / 尚無撥款紀錄的專案會讓整張表空白。）
    /// 已動支 = 四種支出來源加總：
    ///   (1) 請款已撥分期金額（PaymentRequest 非 draft + PaymentRequestInstallment.PaidAt IS NOT NULL）
    ///   (2) 已核准預支沖銷 GrandTotal（透過 AdvanceRequest.ProjectId 回扣專案）
    ///   (3) 出差請款已撥分期金額（TravelPaymentRequest 非 draft + Installment.PaidAt IS NOT NULL）
    ///   (4) 已核准出差沖銷 GrandTotal（透過 TravelRequest.ProjectId 回扣專案）
    /// Percentage 在 C# 端計算，避免 SQL 端除零問題（分母為 0 / NULL 時回 null，前端顯示「—」）。
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

        // 4 個 OUTER APPLY 各自預先聚合一種支出來源，每個專案得到 4 個獨立金額再相加。
        // OUTER APPLY 保證即使該來源無資料也回傳 NULL（外層用 ISNULL → 0）。
        // - 請款 / 出差請款：採已撥（PaidAt IS NOT NULL）的 installments 金額（部分撥款情境精準）
        // - 預支沖銷 / 出差沖銷：採 ApprovalStatus = 'approved' 的 GrandTotal（沒有 installments）
        var sql = $"""
            SELECT p.Id          AS ProjectId,
                   p.Code        AS ProjectCode,
                   p.Name        AS ProjectName,
                   p.Status,
                   d.Name        AS DepartmentName,
                   p.ContractAmount,
                   p.BusinessAmount,
                   p.RemainingAmount,
                   ISNULL(pr_paid.Amount,  0) AS PrPaidAmount,
                   ISNULL(wo.Amount,       0) AS WriteOffAmount,
                   ISNULL(tpr_paid.Amount, 0) AS TprPaidAmount,
                   ISNULL(two.Amount,      0) AS TravelWriteOffAmount
            FROM   Projects p
            LEFT JOIN Departments d ON p.DepartmentId = d.Id
            OUTER APPLY (
              SELECT SUM(i.Amount) AS Amount
              FROM   PaymentRequests pr
              JOIN   PaymentRequestInstallments i ON i.PaymentRequestId = pr.Id
              WHERE  pr.ProjectId = p.Id
                AND  pr.ApprovalStatus <> 'draft'
                AND  i.PaidAt IS NOT NULL
            ) pr_paid
            -- 預支：逐張計算。未結案 = MAX(預支總額, 已核准沖銷)（預支已撥款未沖銷時即佔用水位）；
            -- 已結案 = 結案實際金額（ActualAmount 缺值時退回已核准沖銷合計）
            OUTER APPLY (
              SELECT SUM(CASE WHEN a.IsClosed = 1 THEN ISNULL(a.ActualAmount, x.WoTotal)
                              WHEN ap.Total > x.WoTotal THEN ap.Total
                              ELSE x.WoTotal END) AS Amount
              FROM   AdvanceRequests a
              -- 已核准批次的預支金額：追加預支送簽期間母單轉 pending、GrandTotal 已含未核准批次，
              -- 故以明細依批次加總（進行中的追加批次 RoundNo = CurrentRoundNo 不計）
              OUTER APPLY (
                SELECT ISNULL(SUM(ai.TotalPrice), 0) AS Total
                FROM   AdvanceRequestItems ai
                WHERE  ai.AdvanceRequestId = a.Id
                  AND  (a.ApprovalStatus = 'approved' OR ai.RoundNo < a.CurrentRoundNo)
              ) ap
              OUTER APPLY (
                SELECT ISNULL(SUM(w.GrandTotal), 0) AS WoTotal
                FROM   WriteOffRecords w
                WHERE  w.AdvanceRequestId = a.Id AND w.ApprovalStatus = 'approved'
              ) x
              WHERE  a.ProjectId = p.Id
                AND  (a.ApprovalStatus = 'approved'
                      OR (a.CurrentRoundNo > 1 AND a.ApprovalStatus IN ('pending', 'returned')))
            ) wo
            OUTER APPLY (
              SELECT SUM(i.Amount) AS Amount
              FROM   TravelPaymentRequests tpr
              JOIN   TravelPaymentRequestInstallments i ON i.TravelPaymentRequestId = tpr.Id
              WHERE  tpr.ProjectId = p.Id
                AND  tpr.ApprovalStatus <> 'draft'
                AND  i.PaidAt IS NOT NULL
            ) tpr_paid
            -- 出差預支（不含假日執行活動）：計算方式同上
            OUTER APPLY (
              SELECT SUM(CASE WHEN t.IsClosed = 1 THEN ISNULL(t.ActualAmount, x.WoTotal)
                              WHEN t.GrandTotal > x.WoTotal THEN t.GrandTotal
                              ELSE x.WoTotal END) AS Amount
              FROM   TravelRequests t
              OUTER APPLY (
                SELECT ISNULL(SUM(w.GrandTotal), 0) AS WoTotal
                FROM   TravelWriteOffRecords w
                WHERE  w.TravelRequestId = t.Id AND w.ApprovalStatus = 'approved'
              ) x
              WHERE  t.ProjectId = p.Id
                AND  t.IsHolidayTravel = 0
                AND  t.ApprovalStatus = 'approved'
            ) two
            {scopeClause}
            ORDER BY p.Code
            """;

        var rows = await db.QueryAsync<dynamic>(sql, param);

        return rows.Select(row =>
        {
            decimal prPaid          = (decimal)row.PrPaidAmount;
            decimal writeOff        = (decimal)row.WriteOffAmount;
            decimal tprPaid         = (decimal)row.TprPaidAmount;
            decimal travelWriteOff  = (decimal)row.TravelWriteOffAmount;
            decimal disbursed       = prPaid + writeOff + tprPaid + travelWriteOff;

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
                ? Math.Round(disbursed / businessAmount.Value * 100, 1)
                : null;

            decimal? totalPercentage = (contractAmount.HasValue && contractAmount.Value > 0)
                ? Math.Round((disbursed + preImportUsed) / contractAmount.Value * 100, 1)
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
                DisbursedAmount:     disbursed,
                PreImportUsedAmount: preImportUsed,
                Percentage:          percentage,
                TotalPercentage:     totalPercentage);
        });
    }
}
