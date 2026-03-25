using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class TravelRequestReadService(IDbConnection db) : ITravelRequestReadService
{
    private const string BaseSql = """
        SELECT tr.Id, u.Name AS EmployeeName,
               tr.Destination, tr.StartDate, tr.EndDate,
               tr.GrandTotal, tr.Purpose,
               tr.ProjectId, proj.Code AS ProjectCode, proj.Name AS ProjectName,
               tr.IsHolidayTravel,
               tr.ApprovalStatus, tr.CreatedAt, tr.ReviewedAt, tr.ReviewNote,
               tr.ApprovalItemId, tr.CurrentStepOrder, tr.ReviewedById,
               tr.IsClosed, tr.ClosedAt, tr.RefundAmount,
               tr.EstimatedPaymentDate, tr.PaidAt, tr.EstimatedRefundDate, tr.RefundedAt,
               ti.Id AS ItemId, ti.Category, ti.SeqNo, ti.ItemName,
               ti.UnitPrice, ti.Quantity, ti.TotalPrice,
               ti.Note AS ItemNote, ti.SortOrder
        FROM TravelRequests tr
        LEFT JOIN Users u             ON tr.EmployeeId = u.Id
        LEFT JOIN Projects proj       ON tr.ProjectId  = proj.Id
        LEFT JOIN TravelRequestItems ti ON ti.TravelRequestId = tr.Id
        """;

    public async Task<IEnumerable<TravelRequestDto>> GetAllAsync()
    {
        const string sql = BaseSql + " ORDER BY tr.CreatedAt DESC, ti.SortOrder, ti.Id";
        var rows = await db.QueryAsync<dynamic>(sql);
        return GroupToTravelRequests(rows);
    }

    public async Task<PagedResult<TravelRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null)
    {
        var userFilter = userId.HasValue ? "WHERE SubmittedById = @UserId" : "";
        var countSql   = userId.HasValue
            ? "SELECT COUNT(*) FROM TravelRequests WHERE EmployeeId = @UserId"
            : "SELECT COUNT(*) FROM TravelRequests";
        var sql = $"""
            WITH PagedIds AS (
                SELECT Id FROM TravelRequests
                {(userId.HasValue ? "WHERE EmployeeId = @UserId" : "")}
                ORDER BY CreatedAt DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            )
            {BaseSql} WHERE tr.Id IN (SELECT Id FROM PagedIds) ORDER BY tr.CreatedAt DESC, ti.SortOrder, ti.Id
            """;
        int total = await db.ExecuteScalarAsync<int>(countSql, new { UserId = userId });
        var rows = await db.QueryAsync<dynamic>(sql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize });
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<TravelRequestDto>(
            GroupToTravelRequests(rows),
            total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<TravelRequestDto?> GetByIdAsync(int id)
    {
        var sql = BaseSql + " WHERE tr.Id = @Id ORDER BY ti.SortOrder, ti.Id";
        var rows = await db.QueryAsync<dynamic>(sql, new { Id = id });
        var dto = GroupToTravelRequests(rows).FirstOrDefault();
        if (dto is null) return null;

        // 額外查詢指定審核者（GetByIdAsync 才需要，列表查詢不包含）
        const string drSql = """
            SELECT rdr.Id, rdr.ReviewerId, u.Name AS ReviewerName,
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            WHERE rdr.RequestType = 'travel' AND rdr.RequestId = @RequestId
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

    private static IEnumerable<TravelRequestDto> GroupToTravelRequests(IEnumerable<dynamic> rows)
    {
        var dict = new Dictionary<int, (dynamic tr, List<TravelRequestItemDto> items)>();
        foreach (var row in rows)
        {
            int id = (int)row.Id;
            if (!dict.ContainsKey(id))
                dict[id] = (row, []);
            if (row.ItemId is not null)
                dict[id].items.Add(new TravelRequestItemDto(
                    (int)row.ItemId,
                    (string)row.Category,
                    (int)row.SeqNo,
                    (string)row.ItemName,
                    (decimal)row.UnitPrice,
                    (string)row.Quantity,
                    (decimal)row.TotalPrice,
                    (string?)row.ItemNote,
                    (int)row.SortOrder));
        }

        return dict.Values.Select(x =>
        {
            var tr    = x.tr;
            TravelRequestItemDto[]? items = x.items.Count > 0 ? [.. x.items] : null;
            return new TravelRequestDto(
                (int)tr.Id,
                (string?)tr.EmployeeName ?? "—",
                (string)tr.Destination,
                (DateTime)tr.StartDate,
                (DateTime)tr.EndDate,
                (decimal)tr.GrandTotal,
                (string)tr.Purpose,
                (int?)tr.ProjectId,
                (string?)tr.ProjectCode,
                (string?)tr.ProjectName,
                (bool)tr.IsHolidayTravel,
                (string)tr.ApprovalStatus,
                (DateTime)tr.CreatedAt,
                (DateTime?)tr.ReviewedAt,
                (string?)tr.ReviewNote,
                ApprovalItemId:   (int?)tr.ApprovalItemId,
                CurrentStepOrder: (int?)tr.CurrentStepOrder,
                ReviewedById:     (Guid?)tr.ReviewedById,
                Items:            items,
                IsClosed:                (bool)tr.IsClosed,
                ClosedAt:                (DateTime?)tr.ClosedAt,
                RefundAmount:            (decimal?)tr.RefundAmount,
                EstimatedPaymentDate:    (DateTime?)tr.EstimatedPaymentDate,
                PaidAt:                  (DateTime?)tr.PaidAt,
                EstimatedRefundDate:     (DateTime?)tr.EstimatedRefundDate,
                RefundedAt:              (DateTime?)tr.RefundedAt);
        });
    }
}
