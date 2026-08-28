using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using System.Data;
using System.Text;

namespace Jabez.Api.Services.Dapper;

public sealed class OvertimeReportReadService(IDbConnection db) : IOvertimeReportReadService
{
    /// <summary>
    /// 列表 SQL：以 INNER JOIN Users 確保部門 scope 過濾穩定。count SQL 必須同樣 JOIN，BuildCountFromSql 對應之。
    /// </summary>
    private const string BaseSql = """
        SELECT o.Id, u.Name AS EmployeeName,
               o.OvertimeDate,
               o.EstimatedHours, o.Reason,
               o.CompensationType, o.OvertimePayAmount,
               CASE
                   WHEN a.OvertimeStartTime IS NOT NULL AND a.OvertimeEndTime IS NOT NULL
                   THEN CAST(DATEDIFF(MINUTE, a.OvertimeStartTime, a.OvertimeEndTime) AS DECIMAL(10,2)) / 60.0
                   ELSE NULL
               END AS ActualHours
        FROM OvertimeRequests o
        INNER JOIN Users u ON o.EmployeeId = u.Id
        LEFT JOIN AttendanceRecords a ON o.EmployeeId = a.UserId
            AND CAST(o.OvertimeDate AS DATE) = CAST(a.RecordDate AS DATE)
        """;

    private const string CountFromSql = """
        SELECT COUNT(*) FROM OvertimeRequests o
        INNER JOIN Users u ON o.EmployeeId = u.Id
        """;

    /// <summary>
    /// 依 scope 產生「員工部門」過濾片段（前綴 " AND "）。
    /// SeeAll → 空字串；AllowedIds 為空 → " AND 1=0"；否則 " AND u.DepartmentId IN @AllowedDeptIds"
    /// </summary>
    private static string BuildDeptScopeFilter(ProjectAccessScope scope, DynamicParameters parameters)
    {
        if (scope.SeeAll) return "";
        if (scope.AllowedDepartmentIds.Count == 0) return " AND 1=0";
        parameters.Add("AllowedDeptIds", scope.AllowedDepartmentIds);
        return " AND u.DepartmentId IN @AllowedDeptIds";
    }

    public async Task<PagedResult<OvertimeReportDto>> GetPagedAsync(ProjectAccessScope scope, int page, int pageSize,
        Guid? employeeId = null, int? projectId = null, DateOnly? dateFrom = null, DateOnly? dateTo = null)
    {
        var where = new StringBuilder(" WHERE o.ApprovalStatus = 'approved'");
        var parameters = new DynamicParameters();
        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        where.Append(BuildDeptScopeFilter(scope, parameters));

        if (employeeId.HasValue)
        {
            where.Append(" AND o.EmployeeId = @EmployeeId");
            parameters.Add("EmployeeId", employeeId.Value);
        }

        if (projectId.HasValue)
        {
            // 關聯專案已改子表，以 EXISTS 篩選（走 IX_OvertimeRequestProjects_ProjectId）
            where.Append(" AND EXISTS (SELECT 1 FROM OvertimeRequestProjects orp WHERE orp.OvertimeRequestId = o.Id AND orp.ProjectId = @ProjectId)");
            parameters.Add("ProjectId", projectId.Value);
        }

        // o.OvertimeDate 為 DATE 型別，inclusive 兩端皆可
        if (dateFrom.HasValue)
        {
            where.Append(" AND o.OvertimeDate >= @DateFrom");
            parameters.Add("DateFrom", dateFrom.Value.ToDateTime(TimeOnly.MinValue));
        }

        if (dateTo.HasValue)
        {
            where.Append(" AND o.OvertimeDate <= @DateTo");
            parameters.Add("DateTo", dateTo.Value.ToDateTime(TimeOnly.MinValue));
        }

        var whereClause = where.ToString();

        var countSql = CountFromSql + whereClause;
        int total = await db.ExecuteScalarAsync<int>(countSql, parameters);

        var sql = BaseSql + whereClause +
            " ORDER BY o.OvertimeDate DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        var rows = (await db.QueryAsync<dynamic>(sql, parameters)).ToList();

        // 批次查詢關聯專案明細（共用 OvertimeRequestReadService 的實作，避免兩邊漂移）
        var projectMap = await OvertimeRequestReadService.LoadProjectsAsync(db, rows.Select(r => (int)r.Id));

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<OvertimeReportDto>(
            rows.Select(r => (OvertimeReportDto)MapRow(r, projectMap)),
            total, page, pageSize, Math.Max(1, totalPages));
    }

    private static OvertimeReportDto MapRow(dynamic row, Dictionary<int, OvertimeProjectDto[]> projectMap)
    {
        return new(
            (int)row.Id,
            (string?)row.EmployeeName ?? "—",
            (DateTime)row.OvertimeDate,
            projectMap.GetValueOrDefault((int)row.Id, []),
            (decimal)row.EstimatedHours,
            (decimal?)row.ActualHours,
            (string)row.Reason,
            (string?)row.CompensationType ?? "compensatory",
            (decimal?)row.OvertimePayAmount);
    }
}
