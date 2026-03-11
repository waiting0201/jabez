using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class LeaveRequestReadService(IDbConnection db) : ILeaveRequestReadService
{
    private const string BaseSql = """
        SELECT lr.Id, u.Name AS EmployeeName,
               lr.LeaveType, lr.StartDate, lr.EndDate, lr.Hours, lr.Reason,
               lr.ApprovalStatus, lr.CreatedAt, lr.ReviewedAt, lr.ReviewNote
        FROM LeaveRequests lr
        LEFT JOIN Users u ON lr.EmployeeId = u.Id
        """;

    public async Task<IEnumerable<LeaveRequestDto>> GetAllAsync()
    {
        const string sql = BaseSql + " ORDER BY lr.CreatedAt DESC";
        var rows = await db.QueryAsync<dynamic>(sql);
        return rows.Select(MapRow);
    }

    public async Task<PagedResult<LeaveRequestDto>> GetPagedAsync(int page, int pageSize, Guid userId)
    {
        const string countSql = "SELECT COUNT(*) FROM LeaveRequests WHERE EmployeeId = @UserId";
        const string sql = BaseSql +
            " WHERE lr.EmployeeId = @UserId ORDER BY lr.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
        int total = await db.ExecuteScalarAsync<int>(countSql, new { UserId = userId });
        var rows = await db.QueryAsync<dynamic>(sql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize });
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<LeaveRequestDto>(rows.Select(MapRow), total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<LeaveRequestDto?> GetByIdAsync(int id)
    {
        const string sql = BaseSql + " WHERE lr.Id = @Id";
        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapRow(row);
    }

    private static LeaveRequestDto MapRow(dynamic row) =>
        new(
            (int)row.Id,
            (string?)row.EmployeeName ?? "—",
            (string)row.LeaveType,
            (DateTime)row.StartDate,
            (DateTime)row.EndDate,
            (decimal)row.Hours,
            (string)row.Reason,
            (string)row.ApprovalStatus,
            (DateTime)row.CreatedAt,
            (DateTime?)row.ReviewedAt,
            (string?)row.ReviewNote);
}
