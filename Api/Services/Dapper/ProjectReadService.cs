using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class ProjectReadService(IDbConnection db) : IProjectReadService
{
    private const string SelectSql = """
        SELECT p.Id, p.Code, p.Status, p.DepartmentId, d.Name AS DepartmentName,
               p.BudgetAmount, p.ActualAmount, p.BusinessAmount,
               p.GoogleDriveUrl, p.CreatedAt
        FROM Projects p
        LEFT JOIN Departments d ON p.DepartmentId = d.Id
        """;

    public async Task<IEnumerable<ProjectDto>> GetAllAsync()
    {
        const string sql = SelectSql + " ORDER BY p.CreatedAt DESC";
        var rows = await db.QueryAsync<dynamic>(sql);
        return rows.Select(ToDto);
    }

    public async Task<IEnumerable<ProjectDto>> GetActiveAsync()
    {
        const string sql = SelectSql + " WHERE p.Status = 'active' ORDER BY p.CreatedAt DESC";
        var rows = await db.QueryAsync<dynamic>(sql);
        return rows.Select(ToDto);
    }

    public async Task<PagedResult<ProjectDto>> GetPagedAsync(int page, int pageSize)
    {
        const string countSql = "SELECT COUNT(*) FROM Projects";
        const string sql = SelectSql +
            " ORDER BY p.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
        int total = await db.ExecuteScalarAsync<int>(countSql);
        var rows = await db.QueryAsync<dynamic>(sql, new { Skip = (page - 1) * pageSize, Take = pageSize });
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<ProjectDto>(rows.Select(ToDto), total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<ProjectDto?> GetByIdAsync(int id)
    {
        const string sql = SelectSql + " WHERE p.Id = @Id";
        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : ToDto(row);
    }

    private static ProjectDto ToDto(dynamic row) => new(
        (int)row.Id,
        (string)row.Code,
        (string)row.Status,
        (int?)row.DepartmentId,
        (string?)row.DepartmentName,
        (decimal?)row.BudgetAmount,
        (decimal?)row.ActualAmount,
        (decimal?)row.BusinessAmount,
        (string?)row.GoogleDriveUrl,
        (DateTime)row.CreatedAt);
}
