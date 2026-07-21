using Dapper;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class DepartmentReadService(IDbConnection db) : IDepartmentReadService
{
    public async Task<IEnumerable<DepartmentDto>> GetAllAsync()
    {
        const string sql = """
            SELECT d.Id, d.Name, d.Code, d.Description, d.ParentId,
                   p.Name AS ParentName, d.SortOrder,
                   d.CanViewSiblings, d.CanSeeAll, d.CanViewDescendants, d.CanViewParent,
                   d.CreatedAt,
                   COUNT(u.Id) AS EmployeeCount,
                   (SELECT STRING_AGG(u2.Name, ', ') WITHIN GROUP (ORDER BY ISNULL(jt2.Level, 2147483647), u2.Name)
                      FROM Users u2
                      LEFT JOIN JobTitles jt2 ON u2.JobTitleId = jt2.Id
                      WHERE u2.DepartmentId = d.Id
                        AND u2.IsSuperAdmin = 0) AS EmployeeNames
            FROM Departments d
            LEFT JOIN Departments p ON d.ParentId = p.Id
            LEFT JOIN Users u       ON u.DepartmentId = d.Id
            GROUP BY d.Id, d.Name, d.Code, d.Description, d.ParentId, p.Name, d.SortOrder,
                     d.CanViewSiblings, d.CanSeeAll, d.CanViewDescendants, d.CanViewParent, d.CreatedAt
            ORDER BY d.SortOrder, d.Name
            """;

        var rows = await db.QueryAsync<dynamic>(sql);

        return rows.Select(row => new DepartmentDto(
            (int)row.Id,
            (string)row.Name,
            (string?)row.Code,
            (string?)row.Description,
            (int?)row.ParentId,
            (string?)row.ParentName,
            (int)row.SortOrder,
            (bool)row.CanViewSiblings,
            (bool)row.CanSeeAll,
            (bool)row.CanViewDescendants,
            (bool)row.CanViewParent,
            (int)row.EmployeeCount,
            (DateTime)row.CreatedAt,
            (string?)row.EmployeeNames));
    }

    public async Task<DepartmentDto?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT d.Id, d.Name, d.Code, d.Description, d.ParentId,
                   p.Name AS ParentName, d.SortOrder,
                   d.CanViewSiblings, d.CanSeeAll, d.CanViewDescendants, d.CanViewParent,
                   d.CreatedAt,
                   COUNT(u.Id) AS EmployeeCount,
                   (SELECT STRING_AGG(u2.Name, ', ') WITHIN GROUP (ORDER BY ISNULL(jt2.Level, 2147483647), u2.Name)
                      FROM Users u2
                      LEFT JOIN JobTitles jt2 ON u2.JobTitleId = jt2.Id
                      WHERE u2.DepartmentId = d.Id
                        AND u2.IsSuperAdmin = 0) AS EmployeeNames
            FROM Departments d
            LEFT JOIN Departments p ON d.ParentId = p.Id
            LEFT JOIN Users u       ON u.DepartmentId = d.Id
            WHERE d.Id = @Id
            GROUP BY d.Id, d.Name, d.Code, d.Description, d.ParentId, p.Name, d.SortOrder,
                     d.CanViewSiblings, d.CanSeeAll, d.CanViewDescendants, d.CanViewParent, d.CreatedAt
            """;

        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (row is null) return null;

        return new DepartmentDto(
            (int)row.Id,
            (string)row.Name,
            (string?)row.Code,
            (string?)row.Description,
            (int?)row.ParentId,
            (string?)row.ParentName,
            (int)row.SortOrder,
            (bool)row.CanViewSiblings,
            (bool)row.CanSeeAll,
            (bool)row.CanViewDescendants,
            (bool)row.CanViewParent,
            (int)row.EmployeeCount,
            (DateTime)row.CreatedAt,
            (string?)row.EmployeeNames);
    }
}
