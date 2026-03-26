using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class WriteOffRequestReadService(IDbConnection db) : IWriteOffRequestReadService
{
    // 預支沖銷申請主查詢：WriteOffRecords 關聯 AdvanceRequests、Projects、Users、WriteOffItems
    private const string BaseSql = """
        SELECT wo.Id, wo.RequestNo, wo.AdvanceRequestId, ar.RequestNo AS AdvanceRequestNo,
               wo.WriteOffNo, proj.Code AS ProjectCode, proj.Name AS ProjectName,
               ar.ActivityName, ar.ActivityPeriod,
               wo.CashTotal, wo.CheckTotal, wo.GrandTotal,
               wo.Note, wo.ApprovalStatus,
               sub.Name AS SubmittedBy, wo.CreatedAt,
               wo.ReviewedAt, wo.ReviewNote,
               ar.GrandTotal AS AdvanceGrandTotal,
               ISNULL((SELECT SUM(w2.GrandTotal) FROM WriteOffRecords w2
                       WHERE w2.AdvanceRequestId = wo.AdvanceRequestId
                         AND w2.ApprovalStatus = 'approved'
                         AND w2.Id < wo.Id), 0) AS AdvanceWrittenOffTotal,
               CAST(ISNULL(ar.IsClosed, 0) AS BIT) AS AdvanceIsClosed,
               ar.EstimatedRefundDate, ar.RefundedAt,
               wi.Id AS ItemId, wi.Category, wi.SeqNo, wi.ItemName,
               wi.UnitPrice, wi.Quantity, wi.TotalPrice,
               wi.CashAmount AS ItemCash, wi.CheckAmount AS ItemCheck,
               wi.Note AS ItemNote, wi.InvoiceNo AS ItemInvoiceNo,
               wi.FileName AS ItemFileName, wi.FileUrl AS ItemFileUrl, wi.SortOrder,
               wi.InvoiceDate AS ItemInvoiceDate
        FROM WriteOffRecords wo
        LEFT JOIN AdvanceRequests ar ON wo.AdvanceRequestId = ar.Id
        LEFT JOIN Projects proj      ON ar.ProjectId        = proj.Id
        LEFT JOIN Users sub          ON wo.SubmittedById    = sub.Id
        LEFT JOIN WriteOffItems wi   ON wi.WriteOffRecordId = wo.Id
        """;

    public async Task<PagedResult<WriteOffRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null)
    {
        var userFilter = userId.HasValue ? "WHERE SubmittedById = @UserId" : "";
        var countSql = $"SELECT COUNT(*) FROM WriteOffRecords {userFilter}";
        var sql = $"""
            WITH PagedIds AS (
                SELECT Id FROM WriteOffRecords
                {userFilter}
                ORDER BY CreatedAt DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            )
            {BaseSql} WHERE wo.Id IN (SELECT Id FROM PagedIds) ORDER BY wo.CreatedAt DESC, wi.SortOrder, wi.Id
            """;

        int total = await db.ExecuteScalarAsync<int>(countSql, new { UserId = userId });
        var rows = await db.QueryAsync<dynamic>(sql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize });

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<WriteOffRequestDto>(
            GroupToWriteOffRequests(rows),
            total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<WriteOffRequestDto?> GetByIdAsync(int id)
    {
        var sql = BaseSql + " WHERE wo.Id = @Id ORDER BY wi.SortOrder, wi.Id";
        var rows = await db.QueryAsync<dynamic>(sql, new { Id = id });

        var dto = GroupToWriteOffRequests(rows).FirstOrDefault();
        if (dto is null) return null;

        // 額外查詢指定審核者（GetByIdAsync 才需要，列表查詢不包含）
        const string drSql = """
            SELECT rdr.Id, rdr.ReviewerId, u.Name AS ReviewerName,
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            WHERE rdr.RequestType = 'write_off' AND rdr.RequestId = @RequestId
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

    private static IEnumerable<WriteOffRequestDto> GroupToWriteOffRequests(IEnumerable<dynamic> rows)
    {
        var dict = new Dictionary<int, (dynamic wo, List<WriteOffItemDto> items)>();
        foreach (var row in rows)
        {
            int id = (int)row.Id;
            if (!dict.ContainsKey(id))
                dict[id] = (row, []);
            if (row.ItemId is not null)
                dict[id].items.Add(new WriteOffItemDto(
                    (int)row.ItemId,
                    (string)row.Category,
                    (int)row.SeqNo,
                    (string)row.ItemName,
                    (decimal)row.UnitPrice,
                    (string)row.Quantity,
                    (decimal)row.TotalPrice,
                    (decimal)row.ItemCash,
                    (decimal)row.ItemCheck,
                    (string?)row.ItemNote,
                    (string?)row.ItemInvoiceNo,
                    (string?)row.ItemFileName,
                    (string?)row.ItemFileUrl,
                    (int)row.SortOrder,
                    (DateTime?)row.ItemInvoiceDate));
        }

        return dict.Values.Select(x => new WriteOffRequestDto(
            (int)x.wo.Id,
            (string)x.wo.RequestNo,
            (int)x.wo.AdvanceRequestId,
            (string)x.wo.AdvanceRequestNo,
            (int)x.wo.WriteOffNo,
            (string)x.wo.ProjectCode,
            (string)x.wo.ProjectName,
            (string)x.wo.ActivityName,
            (string)x.wo.ActivityPeriod,
            (decimal)x.wo.CashTotal,
            (decimal)x.wo.CheckTotal,
            (decimal)x.wo.GrandTotal,
            (string?)x.wo.Note,
            (string)x.wo.ApprovalStatus,
            (string?)x.wo.SubmittedBy,
            (DateTime)x.wo.CreatedAt,
            (DateTime?)x.wo.ReviewedAt,
            (string?)x.wo.ReviewNote,
            [.. x.items],
            null, // DesignatedReviewers 以 null 回傳（GetByIdAsync 才填入）
            (decimal)x.wo.AdvanceGrandTotal,
            (decimal)x.wo.AdvanceWrittenOffTotal,
            (bool)x.wo.AdvanceIsClosed,
            (DateTime?)x.wo.EstimatedRefundDate,
            (DateTime?)x.wo.RefundedAt));
    }
}
