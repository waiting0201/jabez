using Dapper;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class JobTitleReadService(IDbConnection db) : IJobTitleReadService
{
    public async Task<IEnumerable<JobTitleDto>> GetAllAsync()
    {
        const string sql = """
            SELECT j.Id, j.Name, j.Level, j.Description, j.CreatedAt,
                   COUNT(u.Id) AS EmployeeCount
            FROM JobTitles j
            LEFT JOIN Users u ON u.JobTitleId = j.Id
            GROUP BY j.Id, j.Name, j.Level, j.Description, j.CreatedAt
            ORDER BY j.Level
            """;

        var rows = await db.QueryAsync<dynamic>(sql);

        return rows.Select(row => new JobTitleDto(
            (int)row.Id,
            (string)row.Name,
            (int)row.Level,
            (string?)row.Description,
            (int)row.EmployeeCount,
            (DateTime)row.CreatedAt));
    }

    /// <summary>輕量級職稱清單（供下拉選單，不需 job-titles:read 權限）</summary>
    public async Task<IEnumerable<JobTitleLookupDto>> GetLookupAsync()
    {
        const string sql = "SELECT Id, Name FROM JobTitles ORDER BY Level";
        return await db.QueryAsync<JobTitleLookupDto>(sql);
    }

    public async Task<JobTitleDto?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT j.Id, j.Name, j.Level, j.Description, j.CreatedAt,
                   COUNT(u.Id) AS EmployeeCount
            FROM JobTitles j
            LEFT JOIN Users u ON u.JobTitleId = j.Id
            WHERE j.Id = @Id
            GROUP BY j.Id, j.Name, j.Level, j.Description, j.CreatedAt
            """;

        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (row is null) return null;

        return new JobTitleDto(
            (int)row.Id,
            (string)row.Name,
            (int)row.Level,
            (string?)row.Description,
            (int)row.EmployeeCount,
            (DateTime)row.CreatedAt);
    }
}
