using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class TravelPaymentRequestReadService(IDbConnection db, IInstallmentReadService installments) : ITravelPaymentRequestReadService
{
    private const string BaseSql = """
        SELECT tpr.Id, tpr.RequestNo, u.Name AS EmployeeName,
               tpr.Destination, tpr.StartDate, tpr.EndDate,
               tpr.GrandTotal, tpr.Purpose,
               tpr.ProjectId, proj.Code AS ProjectCode, proj.Name AS ProjectName,
               tpr.ApprovalStatus, tpr.CreatedAt, tpr.ReviewedAt, tpr.ReviewNote,
               tpr.ApprovalItemId, tpr.CurrentStepOrder, tpr.ReviewedById,
               tpr.EstimatedPaymentDate, tpr.PaidAt,
               ti.Id AS ItemId, ti.Category, ti.SeqNo, ti.ItemName,
               ti.UnitPrice, ti.Quantity, ti.TotalPrice,
               ti.Note AS ItemNote, ti.SortOrder,
               ti.InvoiceNo, ti.FileName AS ItemFileName, ti.FileUrl AS ItemFileUrl, ti.InvoiceDate AS ItemInvoiceDate
        FROM TravelPaymentRequests tpr
        LEFT JOIN Users u               ON tpr.EmployeeId  = u.Id
        LEFT JOIN Projects proj         ON tpr.ProjectId   = proj.Id
        LEFT JOIN TravelPaymentRequestItems ti ON ti.TravelPaymentRequestId = tpr.Id
        """;

    public async Task<PagedResult<TravelPaymentRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null)
    {
        var whereClause = userId.HasValue ? "WHERE EmployeeId = @UserId" : "";
        var countSql = $"SELECT COUNT(*) FROM TravelPaymentRequests {whereClause}";
        var sql = $"""
            WITH PagedIds AS (
                SELECT Id FROM TravelPaymentRequests
                {whereClause}
                ORDER BY CreatedAt DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            )
            {BaseSql} WHERE tpr.Id IN (SELECT Id FROM PagedIds) ORDER BY tpr.CreatedAt DESC, ti.SortOrder, ti.Id
            """;
        var parameters = new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize };
        int total = await db.ExecuteScalarAsync<int>(countSql, parameters);
        var rows = await db.QueryAsync<dynamic>(sql, parameters);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        var dtos = GroupToTravelPaymentRequests(rows).ToList();
        var idsForInst = dtos.Select(d => d.Id).ToList();
        var instDict = await installments.GetByParentIdsAsync(InstallmentParentTable.TravelPaymentRequest, idsForInst);
        var withStatus = dtos.Select(d => d with { PaymentStatus = installments.ComputeStatus(instDict.GetValueOrDefault(d.Id, [])) });
        return new PagedResult<TravelPaymentRequestDto>(
            withStatus,
            total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<TravelPaymentRequestDto?> GetByIdAsync(int id)
    {
        var sql = BaseSql + " WHERE tpr.Id = @Id ORDER BY ti.SortOrder, ti.Id";
        var rows = await db.QueryAsync<dynamic>(sql, new { Id = id });
        var dto = GroupToTravelPaymentRequests(rows).FirstOrDefault();
        if (dto is null) return null;

        // 額外查詢指定審核者（GetByIdAsync 才需要，列表查詢不包含）
        const string drSql = """
            SELECT rdr.Id, rdr.ReviewerId, u.Name AS ReviewerName,
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            WHERE rdr.RequestType = 'travel_payment' AND rdr.RequestId = @RequestId
            ORDER BY rdr.StepOrder
            """;
        var drRows = await db.QueryAsync<dynamic>(drSql, new { RequestId = id });
        var designatedReviewers = drRows.Select(r => new DesignatedReviewerDto(
            (int)r.Id,
            (Guid)r.ReviewerId,
            (string)r.ReviewerName,
            (int)r.StepOrder,
            (string)r.Status,
            (DateTime?)r.ReviewedAt,
            (string?)r.Comment)).ToArray();

        var instDict = await installments.GetByParentIdsAsync(InstallmentParentTable.TravelPaymentRequest, new[] { id });
        var instList = instDict.GetValueOrDefault(id, []);

        return dto with
        {
            DesignatedReviewers = designatedReviewers.Length > 0 ? designatedReviewers : null,
            Installments        = instList.Count > 0 ? instList.ToArray() : null,
            PaymentStatus       = installments.ComputeStatus(instList),
        };
    }

    // ── Grouping helpers ─────────────────────────────────────────────────────

    private static IEnumerable<TravelPaymentRequestDto> GroupToTravelPaymentRequests(IEnumerable<dynamic> rows)
    {
        var dict = new Dictionary<int, (dynamic tpr, List<TravelPaymentRequestItemDto> items)>();
        foreach (var row in rows)
        {
            int id = (int)row.Id;
            if (!dict.ContainsKey(id))
                dict[id] = (row, []);
            if (row.ItemId is not null)
                dict[id].items.Add(new TravelPaymentRequestItemDto(
                    (int)row.ItemId,
                    (string)row.Category,
                    (int)row.SeqNo,
                    (string)row.ItemName,
                    (decimal)row.UnitPrice,
                    (string)row.Quantity,
                    (decimal)row.TotalPrice,
                    (string?)row.ItemNote,
                    (int)row.SortOrder,
                    (string?)row.InvoiceNo,
                    (string?)row.ItemFileName,
                    (string?)row.ItemFileUrl,
                    (DateTime?)row.ItemInvoiceDate));
        }

        return dict.Values.Select(x =>
        {
            var tpr = x.tpr;
            TravelPaymentRequestItemDto[]? items = x.items.Count > 0 ? [.. x.items] : null;
            return new TravelPaymentRequestDto(
                (int)tpr.Id,
                (string)tpr.RequestNo,
                (string?)tpr.EmployeeName ?? "—",
                (string)tpr.Destination,
                (DateTime)tpr.StartDate,
                (DateTime)tpr.EndDate,
                (decimal)tpr.GrandTotal,
                (string)tpr.Purpose,
                (int?)tpr.ProjectId,
                (string?)tpr.ProjectCode,
                (string?)tpr.ProjectName,
                (string)tpr.ApprovalStatus,
                (DateTime)tpr.CreatedAt,
                (DateTime?)tpr.ReviewedAt,
                (string?)tpr.ReviewNote,
                ApprovalItemId:       (int?)tpr.ApprovalItemId,
                CurrentStepOrder:     (int?)tpr.CurrentStepOrder,
                ReviewedById:         (Guid?)tpr.ReviewedById,
                EstimatedPaymentDate: (DateTime?)tpr.EstimatedPaymentDate,
                PaidAt:               (DateTime?)tpr.PaidAt,
                Items:                items);
        });
    }
}
