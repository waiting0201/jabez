using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class AdvanceRequestReadService(IDbConnection db) : IAdvanceRequestReadService
{
    private const string BaseSql = """
        SELECT ar.Id, ar.RequestNo, ar.ProjectId, proj.Code AS ProjectCode, proj.Name AS ProjectName,
               ar.ActivityName, ar.ActivityPeriod, ar.AdvanceDate,
               ar.CashTotal, ar.CheckTotal, ar.GrandTotal,
               ar.ApprovalStatus, ar.EstimatedPaymentDate,
               sub.Name AS SubmittedBy, ar.CreatedAt,
               ar.ReviewedAt, ar.ReviewNote,
               ar.IsClosed, ar.ClosedAt, ar.RefundAmount, ar.RefundedAt,
               ai.Id AS ItemId, ai.Category, ai.SeqNo, ai.ItemName,
               ai.UnitPrice, ai.Quantity, ai.TotalPrice,
               ai.CashAmount AS ItemCash, ai.CheckAmount AS ItemCheck,
               ai.Note AS ItemNote, ai.SortOrder
        FROM AdvanceRequests ar
        LEFT JOIN Projects proj          ON ar.ProjectId    = proj.Id
        LEFT JOIN Users   sub            ON ar.SubmittedById = sub.Id
        LEFT JOIN AdvanceRequestItems ai ON ai.AdvanceRequestId = ar.Id
        """;

    public async Task<PagedResult<AdvanceRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null)
    {
        var userFilter = userId.HasValue ? "WHERE SubmittedById = @UserId" : "";
        var countSql = $"SELECT COUNT(*) FROM AdvanceRequests {userFilter}";
        var sql = $"""
            WITH PagedIds AS (
                SELECT Id FROM AdvanceRequests
                {userFilter}
                ORDER BY CreatedAt DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            )
            {BaseSql} WHERE ar.Id IN (SELECT Id FROM PagedIds) ORDER BY ar.CreatedAt DESC, ai.SortOrder, ai.Id
            """;
        int total = await db.ExecuteScalarAsync<int>(countSql, new { UserId = userId });
        var rows = await db.QueryAsync<dynamic>(sql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize });

        // 取得沖銷摘要
        var ids = rows.Select(r => (int)r.Id).Distinct().ToArray();
        var writeOffSummaries = ids.Length > 0
            ? await db.QueryAsync<dynamic>(
                "SELECT Id, AdvanceRequestId, WriteOffNo, GrandTotal, CreatedAt FROM WriteOffRecords WHERE AdvanceRequestId IN @Ids ORDER BY WriteOffNo",
                new { Ids = ids })
            : Enumerable.Empty<dynamic>();

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<AdvanceRequestDto>(
            GroupToAdvanceRequests(rows, writeOffSummaries),
            total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<AdvanceRequestDto?> GetByIdAsync(int id)
    {
        var sql = BaseSql + " WHERE ar.Id = @Id ORDER BY ai.SortOrder, ai.Id";
        var rows = await db.QueryAsync<dynamic>(sql, new { Id = id });
        var writeOffSummaries = await db.QueryAsync<dynamic>(
            "SELECT Id, AdvanceRequestId, WriteOffNo, GrandTotal, CreatedAt FROM WriteOffRecords WHERE AdvanceRequestId = @Id ORDER BY WriteOffNo",
            new { Id = id });
        var dto = GroupToAdvanceRequests(rows, writeOffSummaries).FirstOrDefault();
        if (dto is null) return null;

        // 額外查詢指定審核者（GetByIdAsync 才需要，列表查詢不包含）
        const string drSql = """
            SELECT rdr.Id, rdr.ReviewerId, u.Name AS ReviewerName,
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            WHERE rdr.RequestType = 'advance' AND rdr.RequestId = @RequestId
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

    private static IEnumerable<AdvanceRequestDto> GroupToAdvanceRequests(
        IEnumerable<dynamic> rows, IEnumerable<dynamic> writeOffRows)
    {
        var dict = new Dictionary<int, (dynamic ar, List<AdvanceRequestItemDto> items)>();
        foreach (var row in rows)
        {
            int id = (int)row.Id;
            if (!dict.ContainsKey(id))
                dict[id] = (row, []);
            if (row.ItemId is not null)
                dict[id].items.Add(new AdvanceRequestItemDto(
                    (int)row.ItemId, (string)row.Category, (int)row.SeqNo,
                    (string)row.ItemName, (decimal)row.UnitPrice, (string)row.Quantity,
                    (decimal)row.TotalPrice, (decimal)row.ItemCash, (decimal)row.ItemCheck,
                    (string?)row.ItemNote, (int)row.SortOrder));
        }

        // 沖銷摘要 grouped by AdvanceRequestId
        var woDict = new Dictionary<int, List<WriteOffSummaryDto>>();
        foreach (var row in writeOffRows)
        {
            int arId = (int)row.AdvanceRequestId;
            if (!woDict.ContainsKey(arId))
                woDict[arId] = [];
            woDict[arId].Add(new WriteOffSummaryDto(
                (int)row.Id, (int)row.WriteOffNo,
                (decimal)row.GrandTotal, (DateTime)row.CreatedAt));
        }

        return dict.Values.Select(x =>
        {
            int id = (int)x.ar.Id;
            var wos = woDict.TryGetValue(id, out var list) ? [.. list] : Array.Empty<WriteOffSummaryDto>();
            return new AdvanceRequestDto(
                id,
                (string)x.ar.RequestNo,
                (int)x.ar.ProjectId,
                (string)x.ar.ProjectCode,
                (string)x.ar.ProjectName,
                (string)x.ar.ActivityName,
                (string)x.ar.ActivityPeriod,
                (DateTime)x.ar.AdvanceDate,
                (decimal)x.ar.CashTotal,
                (decimal)x.ar.CheckTotal,
                (decimal)x.ar.GrandTotal,
                (string)x.ar.ApprovalStatus,
                (string?)x.ar.SubmittedBy,
                (DateTime)x.ar.CreatedAt,
                (DateTime?)x.ar.EstimatedPaymentDate,
                (DateTime?)x.ar.ReviewedAt,
                (string?)x.ar.ReviewNote,
                [.. x.items],
                wos,
                null,                               // DesignatedReviewers 以 null 回傳
                (bool)x.ar.IsClosed,
                (DateTime?)x.ar.ClosedAt,
                (decimal?)x.ar.RefundAmount,
                (DateTime?)x.ar.RefundedAt);
        });
    }

}
