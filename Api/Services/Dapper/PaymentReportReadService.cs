using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using System.Data;
using System.Text;

namespace Jabez.Api.Services.Dapper;

public sealed class PaymentReportReadService(IDbConnection db) : IPaymentReportReadService
{
    /// <summary>
    /// 撥款日推算（沿襲舊 pr.PaidAt 語意）：全撥完 → MAX(installment.PaidAt)，否則 NULL。
    /// 用 CASE 配 EXISTS / NOT EXISTS，避免子查詢回傳多列。
    /// </summary>
    private const string PaidAtSubquery = """
        CASE
          WHEN NOT EXISTS (SELECT 1 FROM PaymentRequestInstallments i WHERE i.PaymentRequestId = pr.Id) THEN NULL
          WHEN EXISTS (SELECT 1 FROM PaymentRequestInstallments i WHERE i.PaymentRequestId = pr.Id AND i.PaidAt IS NULL) THEN NULL
          ELSE (SELECT MAX(i.PaidAt) FROM PaymentRequestInstallments i WHERE i.PaymentRequestId = pr.Id)
        END
        """;

    private static readonly string BaseSql = $"""
        SELECT pr.Id,
               pr.RequestNo,
               u.Name  AS EmployeeName,
               pr.Type,
               proj.Code AS ProjectCode,
               proj.Name AS ProjectName,
               (SELECT STRING_AGG(InvoiceNo, ',')
                FROM InvoiceItems
                WHERE PaymentRequestId = pr.Id) AS InvoiceNos,
               pr.TotalAmount,
               pr.ApprovalStatus,
               {PaidAtSubquery} AS PaidAt,
               pr.CreatedAt
        FROM PaymentRequests pr
        JOIN Users   u    ON pr.SubmittedById = u.Id
        JOIN Projects proj ON pr.ProjectId    = proj.Id
        """;

    /// <summary>
    /// count SQL 必須與 BaseSql 共用 JOIN，否則 WHERE u.DepartmentId / proj 欄位無法使用。
    /// </summary>
    private const string CountFromSql = """
        SELECT COUNT(*) FROM PaymentRequests pr
        JOIN Users   u    ON pr.SubmittedById = u.Id
        JOIN Projects proj ON pr.ProjectId    = proj.Id
        """;

    /// <summary>
    /// 匯出用 SQL：LEFT JOIN InvoiceItems，一張發票一列；無發票仍輸出一列（ii.* 全為 null）。
    /// </summary>
    private static readonly string ExportBaseSql = $"""
        SELECT pr.Id            AS PaymentRequestId,
               pr.RequestNo,
               u.Name           AS EmployeeName,
               pr.Type,
               proj.Code        AS ProjectCode,
               proj.Name        AS ProjectName,
               pr.ApprovalStatus,
               pr.CreatedAt,
               {PaidAtSubquery} AS PaidAt,
               pr.TotalAmount   AS PaymentTotalAmount,
               ii.InvoiceNo,
               ii.ItemName      AS InvoiceItemName,
               ii.InvoiceDate,
               ii.Amount        AS InvoiceAmount
        FROM PaymentRequests pr
        JOIN Users     u    ON pr.SubmittedById = u.Id
        JOIN Projects  proj ON pr.ProjectId    = proj.Id
        LEFT JOIN InvoiceItems ii ON ii.PaymentRequestId = pr.Id
        """;

    /// <summary>
    /// 依 scope 產生「申請人部門」過濾片段（前綴 " AND "）。
    /// SeeAll → 空字串；AllowedIds 為空 → " AND 1=0"；否則 " AND u.DepartmentId IN @AllowedDeptIds"
    /// </summary>
    private static string BuildDeptScopeFilter(ProjectAccessScope scope, DynamicParameters parameters)
    {
        if (scope.SeeAll) return "";
        if (scope.AllowedDepartmentIds.Count == 0) return " AND 1=0";
        parameters.Add("AllowedDeptIds", scope.AllowedDepartmentIds);
        return " AND u.DepartmentId IN @AllowedDeptIds";
    }

    /// <summary>
    /// 共用 WHERE 組裝：排除草稿 + 部門 scope + 日期區間 + 付款狀態。
    /// </summary>
    private static string BuildWhereAndParameters(
        ProjectAccessScope scope,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        string? paymentStatus,
        DynamicParameters parameters)
    {
        var where = new StringBuilder(" WHERE pr.ApprovalStatus != 'draft'");

        where.Append(BuildDeptScopeFilter(scope, parameters));

        // pr.CreatedAt 為 DATETIME；dateFrom 從當日 00:00 起，dateTo 用半開區間涵蓋當日 23:59:59.999
        if (dateFrom.HasValue)
        {
            where.Append(" AND pr.CreatedAt >= @DateFrom");
            parameters.Add("DateFrom", dateFrom.Value.ToDateTime(TimeOnly.MinValue));
        }

        if (dateTo.HasValue)
        {
            where.Append(" AND pr.CreatedAt < DATEADD(day, 1, @DateTo)");
            parameters.Add("DateTo", dateTo.Value.ToDateTime(TimeOnly.MinValue));
        }

        // paymentStatus: 'paid' → 全部撥完（有 installments 且無 PaidAt 為 null）；'unpaid' → 反之
        if (paymentStatus == "paid")
        {
            where.Append(" AND EXISTS (SELECT 1 FROM PaymentRequestInstallments i WHERE i.PaymentRequestId = pr.Id)");
            where.Append(" AND NOT EXISTS (SELECT 1 FROM PaymentRequestInstallments i WHERE i.PaymentRequestId = pr.Id AND i.PaidAt IS NULL)");
        }
        else if (paymentStatus == "unpaid")
        {
            where.Append(" AND (NOT EXISTS (SELECT 1 FROM PaymentRequestInstallments i WHERE i.PaymentRequestId = pr.Id)");
            where.Append("      OR EXISTS (SELECT 1 FROM PaymentRequestInstallments i WHERE i.PaymentRequestId = pr.Id AND i.PaidAt IS NULL))");
        }

        return where.ToString();
    }

    public async Task<PagedResult<PaymentReportDto>> GetPagedAsync(
        ProjectAccessScope scope,
        int page, int pageSize,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? paymentStatus = null)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        var whereClause = BuildWhereAndParameters(scope, dateFrom, dateTo, paymentStatus, parameters);

        var countSql = CountFromSql + whereClause;
        int total = await db.ExecuteScalarAsync<int>(countSql, parameters);

        var sql = BaseSql + whereClause +
            " ORDER BY pr.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        var rows = (await db.QueryAsync<dynamic>(sql, parameters)).ToList();

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        Func<dynamic, PaymentReportDto> mapFn = MapRow;
        return new PagedResult<PaymentReportDto>(
            rows.Select(mapFn), total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<List<PaymentExportRowDto>> GetExportRowsAsync(
        ProjectAccessScope scope,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? paymentStatus = null)
    {
        var parameters = new DynamicParameters();
        var whereClause = BuildWhereAndParameters(scope, dateFrom, dateTo, paymentStatus, parameters);

        var sql = ExportBaseSql + whereClause +
            " ORDER BY pr.CreatedAt DESC, pr.Id DESC, ii.Id ASC";

        var rows = await db.QueryAsync<PaymentExportRowDto>(sql, parameters);
        return rows.AsList();
    }

    /// <summary>將 dynamic 列映射至 PaymentReportDto，處理 STRING_AGG 結果拆分</summary>
    private static PaymentReportDto MapRow(dynamic row)
    {
        // STRING_AGG 可能回傳 null（無發票項目）或逗號分隔字串
        string[] invoiceNos = string.IsNullOrEmpty((string?)row.InvoiceNos)
            ? []
            : ((string)row.InvoiceNos)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new PaymentReportDto(
            Id:             (int)row.Id,
            RequestNo:      (string)row.RequestNo,
            EmployeeName:   (string?)row.EmployeeName ?? "—",
            Type:           (string)row.Type,
            ProjectCode:    (string?)row.ProjectCode ?? "—",
            ProjectName:    (string?)row.ProjectName ?? "—",
            InvoiceNos:     invoiceNos,
            TotalAmount:    (decimal)row.TotalAmount,
            ApprovalStatus: (string)row.ApprovalStatus,
            PaidAt:         (DateTime?)row.PaidAt,
            CreatedAt:      (DateTime)row.CreatedAt);
    }
}
