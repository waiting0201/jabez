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
               tr.EstimatedCost, tr.Purpose,
               tr.ProjectId, proj.Code AS ProjectCode, proj.Name AS ProjectName,
               tr.IsHolidayTravel,
               tr.ApprovalStatus, tr.CreatedAt, tr.ReviewedAt, tr.ReviewNote,
               tr.ApprovalItemId, tr.CurrentStepOrder, tr.ReviewedById
        FROM TravelRequests tr
        LEFT JOIN Users u       ON tr.EmployeeId = u.Id
        LEFT JOIN Projects proj ON tr.ProjectId  = proj.Id
        """;

    public async Task<IEnumerable<TravelRequestDto>> GetAllAsync()
    {
        const string sql = BaseSql + " ORDER BY tr.CreatedAt DESC";
        var rows = await db.QueryAsync<dynamic>(sql);
        return rows.Select(MapRow);
    }

    public async Task<PagedResult<TravelRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null)
    {
        var userFilter = userId.HasValue ? "WHERE EmployeeId = @UserId" : "";
        var countSql = $"SELECT COUNT(*) FROM TravelRequests {userFilter}";
        var sql = BaseSql +
            (userId.HasValue ? " WHERE tr.EmployeeId = @UserId" : "") +
            " ORDER BY tr.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
        int total = await db.ExecuteScalarAsync<int>(countSql, new { UserId = userId });
        var rows = await db.QueryAsync<dynamic>(sql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize });
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<TravelRequestDto>(rows.Select(MapRow), total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<TravelRequestDto?> GetByIdAsync(int id)
    {
        const string sql = BaseSql + " WHERE tr.Id = @Id";
        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (row is null) return null;

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

        TravelRequestDto dto = MapRow(row);
        return dto with { DesignatedReviewers = designatedReviewers.Length > 0 ? designatedReviewers : null };
    }

    private static TravelRequestDto MapRow(dynamic row) =>
        new(
            (int)row.Id,
            (string?)row.EmployeeName ?? "—",
            (string)row.Destination,
            (DateTime)row.StartDate,
            (DateTime)row.EndDate,
            (decimal)row.EstimatedCost,
            (string)row.Purpose,
            (int?)row.ProjectId,
            (string?)row.ProjectCode,
            (string?)row.ProjectName,
            (bool)row.IsHolidayTravel,
            (string)row.ApprovalStatus,
            (DateTime)row.CreatedAt,
            (DateTime?)row.ReviewedAt,
            (string?)row.ReviewNote,
            ApprovalItemId:   (int?)row.ApprovalItemId,
            CurrentStepOrder: (int?)row.CurrentStepOrder,
            ReviewedById:     (Guid?)row.ReviewedById);
}
