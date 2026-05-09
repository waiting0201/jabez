using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class ProjectReadService(IDbConnection db) : IProjectReadService
{
    private const string SelectSql = """
        SELECT p.Id, p.Code, p.Name, p.Status, p.StartDate, p.EndDate,
               p.DepartmentId, d.Name AS DepartmentName,
               ISNULL((SELECT SUM(s.DepositAmount)
                       FROM ProjectPaymentSchedules s
                       WHERE s.ProjectId = p.Id), 0) AS ReceivedAmount,
               p.ContractAmount, p.BusinessAmount, p.RemainingAmount,
               p.GoogleDriveUrl, p.CreatedAt
        FROM Projects p
        LEFT JOIN Departments d ON p.DepartmentId = d.Id
        """;

    /// <summary>
    /// 依 scope 組合 WHERE 片段與 Dapper 參數：
    /// scope.SeeAll=true → 不加條件
    /// scope.SeeAll=false + AllowedIds 非空 → WHERE p.DepartmentId IN @AllowedIds
    /// scope.SeeAll=false + AllowedIds 為空 → WHERE 1 = 0（空集合）
    /// </summary>
    private static (string clause, DynamicParameters param) BuildScopeFilter(ProjectAccessScope scope)
    {
        var p = new DynamicParameters();
        if (scope.SeeAll)
            return ("", p);
        if (scope.AllowedDepartmentIds.Count == 0)
            return ("1 = 0", p);
        p.Add("AllowedDeptIds", scope.AllowedDepartmentIds);
        return ("p.DepartmentId IN @AllowedDeptIds", p);
    }

    public async Task<IEnumerable<ProjectDto>> GetAllAsync(ProjectAccessScope scope)
    {
        var (scopeClause, param) = BuildScopeFilter(scope);
        var where = string.IsNullOrEmpty(scopeClause) ? "" : " WHERE " + scopeClause;
        var sql = SelectSql + where + " ORDER BY p.CreatedAt DESC";
        var rows = await db.QueryAsync<dynamic>(sql, param);
        return rows.Select(r => (ProjectDto)ToDto(r, null));
    }

    public async Task<IEnumerable<ProjectDto>> GetActiveAsync(ProjectAccessScope scope)
    {
        var (scopeClause, param) = BuildScopeFilter(scope);
        var where = string.IsNullOrEmpty(scopeClause)
            ? " WHERE p.Status = 'active'"
            : " WHERE p.Status = 'active' AND " + scopeClause;
        var sql = SelectSql + where + " ORDER BY p.CreatedAt DESC";
        var rows = await db.QueryAsync<dynamic>(sql, param);
        return rows.Select(r => (ProjectDto)ToDto(r, null));
    }

    public async Task<PagedResult<ProjectDto>> GetPagedAsync(ProjectAccessScope scope, int page, int pageSize, string? search = null, int? year = null, string? status = null)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var hasStatus = !string.IsNullOrWhiteSpace(status);
        var conditions = new List<string>();
        if (hasSearch) conditions.Add("(p.Code LIKE @Search OR p.Name LIKE @Search)");
        if (year.HasValue) conditions.Add("YEAR(p.StartDate) = @Year");
        if (hasStatus) conditions.Add("p.Status = @Status");

        var (scopeClause, scopeParam) = BuildScopeFilter(scope);
        if (!string.IsNullOrEmpty(scopeClause)) conditions.Add(scopeClause);

        var where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : "";
        var searchParam = hasSearch ? $"%{search!.Trim()}%" : null;

        var countSql = "SELECT COUNT(*) FROM Projects p" + where;
        var sql = SelectSql + where +
            " ORDER BY p.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        scopeParam.Add("Search", searchParam);
        scopeParam.Add("Year",   year);
        scopeParam.Add("Status", status);
        scopeParam.Add("Skip",   (page - 1) * pageSize);
        scopeParam.Add("Take",   pageSize);

        int total = await db.ExecuteScalarAsync<int>(countSql, scopeParam);
        var rows = await db.QueryAsync<dynamic>(sql, scopeParam);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<ProjectDto>(rows.Select(r => (ProjectDto)ToDto(r, null)), total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<IEnumerable<int>> GetYearsAsync()
    {
        const string sql = "SELECT DISTINCT YEAR(StartDate) AS Y FROM Projects ORDER BY Y DESC";
        return await db.QueryAsync<int>(sql);
    }

    public async Task<ProjectDto?> GetByIdAsync(int id, ProjectAccessScope scope)
    {
        var (scopeClause, param) = BuildScopeFilter(scope);
        var scopeFragment = string.IsNullOrEmpty(scopeClause) ? "" : " AND " + scopeClause;
        var sql = SelectSql + " WHERE p.Id = @Id" + scopeFragment;
        param.Add("Id", id);
        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, param);
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
        (int)row.DepartmentId,
        (string?)row.DepartmentName,
        (decimal?)row.ReceivedAmount,
        (decimal?)row.ContractAmount,
        (decimal?)row.BusinessAmount,
        (decimal?)row.RemainingAmount,
        (string?)row.GoogleDriveUrl,
        (DateTime)row.CreatedAt,
        schedules ?? []);
}
