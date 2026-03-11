using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;
using System.Text;

namespace Jabez.Api.Services.Dapper;

public sealed class OvertimeReportReadService(IDbConnection db) : IOvertimeReportReadService
{
    private const string BaseSql = """
        SELECT o.Id, u.Name AS EmployeeName,
               o.OvertimeDate, o.ProjectIds,
               o.EstimatedHours, o.Reason,
               CASE
                   WHEN a.OvertimeStartTime IS NOT NULL AND a.OvertimeEndTime IS NOT NULL
                   THEN CAST(DATEDIFF(MINUTE, a.OvertimeStartTime, a.OvertimeEndTime) AS DECIMAL(10,2)) / 60.0
                   ELSE NULL
               END AS ActualHours
        FROM OvertimeRequests o
        LEFT JOIN Users u ON o.EmployeeId = u.Id
        LEFT JOIN AttendanceRecords a ON o.EmployeeId = a.UserId
            AND CAST(o.OvertimeDate AS DATE) = CAST(a.RecordDate AS DATE)
        """;

    public async Task<PagedResult<OvertimeReportDto>> GetPagedAsync(int page, int pageSize,
        Guid? employeeId = null, int? projectId = null, int? year = null, int? month = null)
    {
        var where = new StringBuilder(" WHERE o.ApprovalStatus = 'approved'");
        var parameters = new DynamicParameters();
        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        if (employeeId.HasValue)
        {
            where.Append(" AND o.EmployeeId = @EmployeeId");
            parameters.Add("EmployeeId", employeeId.Value);
        }

        if (projectId.HasValue)
        {
            // ProjectIds 為逗號分隔字串，用 LIKE 搜尋（處理首位、中間、末位）
            where.Append(" AND (',' + o.ProjectIds + ',' LIKE '%,' + CAST(@ProjectId AS VARCHAR) + ',%')");
            parameters.Add("ProjectId", projectId.Value);
        }

        if (year.HasValue)
        {
            where.Append(" AND YEAR(o.OvertimeDate) = @Year");
            parameters.Add("Year", year.Value);
        }

        if (month.HasValue)
        {
            where.Append(" AND MONTH(o.OvertimeDate) = @Month");
            parameters.Add("Month", month.Value);
        }

        var whereClause = where.ToString();

        var countSql = "SELECT COUNT(*) FROM OvertimeRequests o" + whereClause;
        int total = await db.ExecuteScalarAsync<int>(countSql, parameters);

        var sql = BaseSql + whereClause +
            " ORDER BY o.OvertimeDate DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        var rows = (await db.QueryAsync<dynamic>(sql, parameters)).ToList();

        // 批次查詢 ProjectCodes
        var codeMap = await GetProjectCodeMapAsync(rows.Select(r => (string?)r.ProjectIds));

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<OvertimeReportDto>(
            rows.Select(r => (OvertimeReportDto)MapRow(r, codeMap)),
            total, page, pageSize, Math.Max(1, totalPages));
    }

    /// <summary>解析逗號分隔的 ProjectIds 字串為 int 陣列</summary>
    private static int[] ParseIds(string? csv) =>
        string.IsNullOrEmpty(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                 .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                 .Where(id => id > 0)
                 .ToArray();

    /// <summary>根據一批 ProjectIds 字串批次查詢對應 Code</summary>
    private async Task<Dictionary<int, string>> GetProjectCodeMapAsync(IEnumerable<string?> allProjectIds)
    {
        var ids = allProjectIds
            .Where(s => !string.IsNullOrEmpty(s))
            .SelectMany(s => s!.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0) return new();

        const string sql = "SELECT Id, Code FROM Projects WHERE Id IN @Ids";
        var rows = await db.QueryAsync<(int Id, string Code)>(sql, new { Ids = ids });
        return rows.ToDictionary(r => r.Id, r => r.Code);
    }

    private static OvertimeReportDto MapRow(dynamic row, Dictionary<int, string> codeMap)
    {
        var ids = ParseIds((string?)row.ProjectIds);
        var codes = ids.Select(id => codeMap.TryGetValue(id, out var c) ? c : $"#{id}").ToArray();
        return new(
            (int)row.Id,
            (string?)row.EmployeeName ?? "—",
            (DateTime)row.OvertimeDate,
            codes.Length > 0 ? codes : null,
            (decimal)row.EstimatedHours,
            (decimal?)row.ActualHours,
            (string)row.Reason);
    }
}
