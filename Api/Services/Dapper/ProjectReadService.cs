using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class ProjectReadService(IDbConnection db) : IProjectReadService
{
    private const string SelectSql = """
        SELECT p.Id, p.Code, p.Name, p.Status, p.StartDate, p.EndDate,
               p.DepartmentId, d.Name AS DepartmentName,
               p.ReceivedAmount, p.ContractAmount, p.BusinessAmount,
               p.GoogleDriveUrl, p.CreatedAt
        FROM Projects p
        LEFT JOIN Departments d ON p.DepartmentId = d.Id
        """;

    public async Task<IEnumerable<ProjectDto>> GetAllAsync()
    {
        const string sql = SelectSql + " ORDER BY p.CreatedAt DESC";
        var rows = await db.QueryAsync<dynamic>(sql);
        return rows.Select(r => (ProjectDto)ToDto(r, null));
    }

    public async Task<IEnumerable<ProjectDto>> GetActiveAsync()
    {
        const string sql = SelectSql + " WHERE p.Status = 'active' ORDER BY p.CreatedAt DESC";
        var rows = await db.QueryAsync<dynamic>(sql);
        return rows.Select(r => (ProjectDto)ToDto(r, null));
    }

    public async Task<PagedResult<ProjectDto>> GetPagedAsync(int page, int pageSize, string? search = null, int? year = null, string? status = null)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var hasStatus = !string.IsNullOrWhiteSpace(status);
        var conditions = new List<string>();
        if (hasSearch) conditions.Add("(p.Code LIKE @Search OR p.Name LIKE @Search)");
        if (year.HasValue) conditions.Add("YEAR(p.StartDate) = @Year");
        if (hasStatus) conditions.Add("p.Status = @Status");
        var where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : "";
        var searchParam = hasSearch ? $"%{search!.Trim()}%" : null;

        var countSql = "SELECT COUNT(*) FROM Projects p" + where;
        var sql = SelectSql + where +
            " ORDER BY p.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        var param = new { Search = searchParam, Year = year, Status = status, Skip = (page - 1) * pageSize, Take = pageSize };
        int total = await db.ExecuteScalarAsync<int>(countSql, param);
        var rows = await db.QueryAsync<dynamic>(sql, param);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<ProjectDto>(rows.Select(r => (ProjectDto)ToDto(r, null)), total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<IEnumerable<int>> GetYearsAsync()
    {
        const string sql = "SELECT DISTINCT YEAR(StartDate) AS Y FROM Projects ORDER BY Y DESC";
        return await db.QueryAsync<int>(sql);
    }

    public async Task<ProjectDto?> GetByIdAsync(int id)
    {
        const string sql = SelectSql + " WHERE p.Id = @Id";
        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (row is null) return null;

        var schedules = await LoadSchedulesAsync(id);
        return ToDto(row, schedules);
    }

    private async Task<IReadOnlyList<ProjectPaymentScheduleDto>> LoadSchedulesAsync(int projectId)
    {
        const string sql = """
            SELECT Id, PeriodNo, BillingDate, BillingAmount,
                   InvoiceDate, InvoiceAmount, DepositDate, DepositAmount,
                   DeductionNote
            FROM ProjectPaymentSchedules
            WHERE ProjectId = @ProjectId
            ORDER BY PeriodNo
        """;
        var rows = await db.QueryAsync<ProjectPaymentScheduleDto>(sql, new { ProjectId = projectId });
        return rows.ToList();
    }

    private static ProjectDto ToDto(dynamic row, IReadOnlyList<ProjectPaymentScheduleDto>? schedules = null) => new(
        (int)row.Id,
        (string)row.Code,
        (string)row.Name,
        (string)row.Status,
        (DateTime)row.StartDate,
        (DateTime?)row.EndDate,
        (int?)row.DepartmentId,
        (string?)row.DepartmentName,
        (decimal?)row.ReceivedAmount,
        (decimal?)row.ContractAmount,
        (decimal?)row.BusinessAmount,
        (string?)row.GoogleDriveUrl,
        (DateTime)row.CreatedAt,
        schedules ?? []);
}
