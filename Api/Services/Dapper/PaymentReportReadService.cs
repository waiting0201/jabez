using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;
using System.Text;

namespace Jabez.Api.Services.Dapper;

public sealed class PaymentReportReadService(IDbConnection db) : IPaymentReportReadService
{
    private const string BaseSql = """
        SELECT pr.Id,
               u.Name  AS EmployeeName,
               pr.Type,
               proj.Code AS ProjectCode,
               (SELECT STRING_AGG(InvoiceNo, ',')
                FROM InvoiceItems
                WHERE PaymentRequestId = pr.Id) AS InvoiceNos,
               pr.TotalAmount,
               pr.ApprovalStatus,
               pr.PaidAt,
               pr.CreatedAt
        FROM PaymentRequests pr
        JOIN Users   u    ON pr.SubmittedById = u.Id
        JOIN Projects proj ON pr.ProjectId    = proj.Id
        """;

    public async Task<PagedResult<PaymentReportDto>> GetPagedAsync(
        int page, int pageSize,
        int? year = null, int? month = null, string? paymentStatus = null)
    {
        // 排除草稿狀態的基礎 WHERE，僅回傳已送出的申請
        var where = new StringBuilder(" WHERE pr.ApprovalStatus != 'draft'");
        var parameters = new DynamicParameters();
        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        if (year.HasValue)
        {
            where.Append(" AND YEAR(pr.CreatedAt) = @Year");
            parameters.Add("Year", year.Value);
        }

        if (month.HasValue)
        {
            where.Append(" AND MONTH(pr.CreatedAt) = @Month");
            parameters.Add("Month", month.Value);
        }

        // paymentStatus: 'paid' → PaidAt IS NOT NULL；'unpaid' → PaidAt IS NULL
        if (paymentStatus == "paid")
        {
            where.Append(" AND pr.PaidAt IS NOT NULL");
        }
        else if (paymentStatus == "unpaid")
        {
            where.Append(" AND pr.PaidAt IS NULL");
        }

        var whereClause = where.ToString();

        var countSql = "SELECT COUNT(*) FROM PaymentRequests pr" + whereClause;
        int total = await db.ExecuteScalarAsync<int>(countSql, parameters);

        var sql = BaseSql + whereClause +
            " ORDER BY pr.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        var rows = (await db.QueryAsync<dynamic>(sql, parameters)).ToList();

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        Func<dynamic, PaymentReportDto> mapFn = MapRow;
        return new PagedResult<PaymentReportDto>(
            rows.Select(mapFn), total, page, pageSize, Math.Max(1, totalPages));
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
            EmployeeName:   (string?)row.EmployeeName ?? "—",
            Type:           (string)row.Type,
            ProjectCode:    (string?)row.ProjectCode ?? "—",
            InvoiceNos:     invoiceNos,
            TotalAmount:    (decimal)row.TotalAmount,
            ApprovalStatus: (string)row.ApprovalStatus,
            PaidAt:         (DateTime?)row.PaidAt,
            CreatedAt:      (DateTime)row.CreatedAt);
    }
}
