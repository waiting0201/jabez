using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class ShiftChangeRequestReadService(IDbConnection db) : IShiftChangeRequestReadService
{
    private const string BaseSql = """
        SELECT sc.Id, sc.RequestNo, sc.EmployeeId, u.Name AS EmployeeName, d.Name AS DepartmentName,
               sc.Year, sc.Month, sc.Reason, sc.ApprovalStatus, sc.CreatedAt, sc.SubmittedAt,
               sc.ReviewedAt, sc.ReviewNote, sc.ApprovalItemId, sc.CurrentStepOrder, sc.ReviewedById
        FROM ShiftChangeRequests sc
        LEFT JOIN Users u ON sc.EmployeeId = u.Id
        LEFT JOIN Departments d ON u.DepartmentId = d.Id
        """;

    public async Task<PagedResult<ShiftChangeRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null)
    {
        var countSql = "SELECT COUNT(*) FROM ShiftChangeRequests" + (userId.HasValue ? " WHERE EmployeeId = @UserId" : "");
        var sql = BaseSql +
            (userId.HasValue ? " WHERE sc.EmployeeId = @UserId" : "") +
            " ORDER BY COALESCE(sc.SubmittedAt, sc.CreatedAt) DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        int total = await db.ExecuteScalarAsync<int>(countSql, new { UserId = userId });
        var rows = await db.QueryAsync<dynamic>(sql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize });

        List<ShiftChangeRequestDto> items = [.. rows.Select(row => (ShiftChangeRequestDto)MapRow(row, null))];

        // 逐日明細一次撈回（列表也要顯示「調整 N 天」）
        if (items.Count > 0)
        {
            var byRequest = await GetDatesAsync([.. items.Select(i => i.Id)]);
            items = [.. items.Select(i => i with
            {
                Dates = byRequest.TryGetValue(i.Id, out var ds) ? [.. ds] : []
            })];
        }

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<ShiftChangeRequestDto>(items, total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<ShiftChangeRequestDto?> GetByIdAsync(int id)
    {
        const string sql = BaseSql + " WHERE sc.Id = @Id";
        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (row is null) return null;

        var dates = (await GetDatesAsync([id])).TryGetValue(id, out var ds) ? ds : new List<ShiftChangeDateDto>();

        const string drSql = """
            SELECT rdr.Id, rdr.ReviewerId, u.Name AS ReviewerName,
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            WHERE rdr.RequestType = 'shift_change' AND rdr.RequestId = @RequestId
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

        ShiftChangeRequestDto dto = MapRow(row, dates.ToArray());
        return dto with { DesignatedReviewers = designatedReviewers.Length > 0 ? designatedReviewers : null };
    }

    private async Task<Dictionary<int, List<ShiftChangeDateDto>>> GetDatesAsync(int[] ids)
    {
        const string sql = """
            SELECT ShiftChangeRequestId, Date, FromDayType, ToDayType
            FROM ShiftChangeRequestDates
            WHERE ShiftChangeRequestId IN @Ids
            ORDER BY Date
            """;
        var rows = await db.QueryAsync<dynamic>(sql, new { Ids = ids });
        return rows
            .GroupBy(r => (int)r.ShiftChangeRequestId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new ShiftChangeDateDto(
                    (DateTime)r.Date, (string)r.FromDayType, (string)r.ToDayType)).ToList());
    }

    private static ShiftChangeRequestDto MapRow(dynamic row, ShiftChangeDateDto[]? dates) => new(
        (int)row.Id,
        (string?)row.RequestNo,
        (Guid?)row.EmployeeId,
        (string?)row.EmployeeName ?? "—",
        (string?)row.DepartmentName,
        (int)row.Year,
        (int)row.Month,
        (string)row.Reason,
        (string)row.ApprovalStatus,
        (DateTime)row.CreatedAt,
        (DateTime?)row.SubmittedAt,
        (DateTime?)row.ReviewedAt,
        (string?)row.ReviewNote,
        ApprovalItemId:   (int?)row.ApprovalItemId,
        CurrentStepOrder: (int?)row.CurrentStepOrder,
        ReviewedById:     (Guid?)row.ReviewedById,
        Dates:            dates);
}
