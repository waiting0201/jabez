using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class TravelWriteOffRequestReadService(IDbConnection db) : ITravelWriteOffRequestReadService
{
    // 出差沖銷申請主查詢：TravelWriteOffRecords 關聯 TravelRequests、Projects、Users、TravelWriteOffItems
    private const string BaseSql = """
        SELECT two.Id, two.RequestNo, two.TravelRequestId, CAST(tr.Id AS NVARCHAR(20)) AS TravelRequestNo,
               two.WriteOffNo, tr.Destination, tr.StartDate, tr.EndDate, tr.Purpose,
               proj.Code AS ProjectCode, proj.Name AS ProjectName,
               two.GrandTotal, two.Note, two.ApprovalStatus,
               sub.Name AS SubmittedBy, two.CreatedAt,
               two.ReviewedAt, two.ReviewNote,
               tr.GrandTotal AS TravelGrandTotal,
               CAST(ISNULL(tr.IsClosed, 0) AS BIT) AS TravelIsClosed,
               tr.EstimatedRefundDate, tr.RefundedAt,
               tr.RefundAmount AS TravelRefundAmount,
               tr.RefundedAmount AS TravelRefundedAmount,
               ISNULL((SELECT SUM(w2.GrandTotal) FROM TravelWriteOffRecords w2
                       WHERE w2.TravelRequestId = two.TravelRequestId
                         AND w2.ApprovalStatus = 'approved'
                         AND w2.Id < two.Id), 0) AS TravelWrittenOffTotal,
               twi.Id AS ItemId, twi.Category, twi.SeqNo, twi.ItemName,
               twi.UnitPrice, twi.Quantity, twi.TotalPrice,
               twi.Note AS ItemNote, twi.InvoiceNo AS ItemInvoiceNo,
               twi.FileName AS ItemFileName, twi.FileUrl AS ItemFileUrl, twi.SortOrder,
               twi.InvoiceDate AS ItemInvoiceDate
        FROM TravelWriteOffRecords two
        LEFT JOIN TravelRequests tr       ON two.TravelRequestId = tr.Id
        LEFT JOIN Projects proj           ON tr.ProjectId         = proj.Id
        LEFT JOIN Users sub               ON two.SubmittedById    = sub.Id
        LEFT JOIN TravelWriteOffItems twi ON twi.TravelWriteOffRecordId = two.Id
        """;

    public async Task<PagedResult<TravelWriteOffRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null)
    {
        var userFilter = userId.HasValue ? "WHERE SubmittedById = @UserId" : "";
        var countSql = $"SELECT COUNT(*) FROM TravelWriteOffRecords {userFilter}";
        var sql = $"""
            WITH PagedIds AS (
                SELECT Id FROM TravelWriteOffRecords
                {userFilter}
                ORDER BY CreatedAt DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            )
            {BaseSql} WHERE two.Id IN (SELECT Id FROM PagedIds) ORDER BY two.CreatedAt DESC, twi.SortOrder, twi.Id
            """;

        int total = await db.ExecuteScalarAsync<int>(countSql, new { UserId = userId });
        var rows = await db.QueryAsync<dynamic>(sql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize });

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<TravelWriteOffRequestDto>(
            GroupToTravelWriteOffRequests(rows),
            total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<TravelWriteOffRequestDto?> GetByIdAsync(int id)
    {
        var sql = BaseSql + " WHERE two.Id = @Id ORDER BY twi.SortOrder, twi.Id";
        var rows = await db.QueryAsync<dynamic>(sql, new { Id = id });

        var dto = GroupToTravelWriteOffRequests(rows).FirstOrDefault();
        if (dto is null) return null;

        // 額外查詢指定審核者（GetByIdAsync 才需要，列表查詢不包含）
        const string drSql = """
            SELECT rdr.Id, rdr.ReviewerId, u.Name AS ReviewerName,
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            WHERE rdr.RequestType = 'travel_write_off' AND rdr.RequestId = @RequestId
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

        return dto with { DesignatedReviewers = designatedReviewers.Length > 0 ? designatedReviewers : null };
    }

    // ── Grouping helpers ─────────────────────────────────────────────────────

    private static IEnumerable<TravelWriteOffRequestDto> GroupToTravelWriteOffRequests(IEnumerable<dynamic> rows)
    {
        var dict = new Dictionary<int, (dynamic two, List<TravelWriteOffItemDto> items)>();
        foreach (var row in rows)
        {
            int id = (int)row.Id;
            if (!dict.ContainsKey(id))
                dict[id] = (row, []);
            if (row.ItemId is not null)
                dict[id].items.Add(new TravelWriteOffItemDto(
                    (int)row.ItemId,
                    (string)row.Category,
                    (int)row.SeqNo,
                    (string)row.ItemName,
                    (decimal)row.UnitPrice,
                    (string)row.Quantity,
                    (decimal)row.TotalPrice,
                    (string?)row.ItemNote,
                    (string?)row.ItemInvoiceNo,
                    (string?)row.ItemFileName,
                    (string?)row.ItemFileUrl,
                    (int)row.SortOrder,
                    (DateTime?)row.ItemInvoiceDate));
        }

        return dict.Values.Select(x => new TravelWriteOffRequestDto(
            (int)x.two.Id,
            (string)x.two.RequestNo,
            (int)x.two.TravelRequestId,
            (string)x.two.TravelRequestNo,
            (int)x.two.WriteOffNo,
            (string)x.two.Destination,
            (DateTime)x.two.StartDate,
            (DateTime)x.two.EndDate,
            (string)x.two.Purpose,
            (string?)x.two.ProjectCode ?? "",
            (string?)x.two.ProjectName ?? "",
            (decimal)x.two.GrandTotal,
            (string?)x.two.Note,
            (string)x.two.ApprovalStatus,
            (string?)x.two.SubmittedBy,
            (DateTime)x.two.CreatedAt,
            (DateTime?)x.two.ReviewedAt,
            (string?)x.two.ReviewNote,
            [.. x.items],
            null, // DesignatedReviewers 以 null 回傳（GetByIdAsync 才填入）
            (decimal)x.two.TravelGrandTotal,
            (decimal)x.two.TravelWrittenOffTotal,
            (bool)x.two.TravelIsClosed,
            (DateTime?)x.two.EstimatedRefundDate,
            (DateTime?)x.two.RefundedAt,
            (decimal?)x.two.TravelRefundAmount,
            (decimal?)x.two.TravelRefundedAmount));
    }
}
