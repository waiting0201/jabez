using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class OvertimeRequestReadService(IDbConnection db) : IOvertimeRequestReadService
{
    private const string BaseSql = """
        SELECT o.Id, u.Name AS EmployeeName,
               o.OvertimeDate, o.ProjectIds,
               o.EstimatedHours, o.Reason,
               o.ApprovalStatus, o.CreatedAt, o.ReviewedAt, o.ReviewNote,
               o.DesignatedReviewerId, dr.Name AS DesignatedReviewerName
        FROM OvertimeRequests o
        LEFT JOIN Users u  ON o.EmployeeId          = u.Id
        LEFT JOIN Users dr ON o.DesignatedReviewerId = dr.Id
        """;

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

    private static OvertimeRequestDto MapRow(dynamic row, Dictionary<int, string> codeMap)
    {
        var ids = ParseIds((string?)row.ProjectIds);
        var codes = ids.Select(id => codeMap.TryGetValue(id, out var c) ? c : $"#{id}").ToArray();
        return new(
            (int)row.Id,
            (string?)row.EmployeeName ?? "—",
            (DateTime)row.OvertimeDate,
            ids.Length > 0 ? ids : null,
            codes.Length > 0 ? codes : null,
            (decimal)row.EstimatedHours,
            (string)row.Reason,
            (string)row.ApprovalStatus,
            (DateTime)row.CreatedAt,
            (DateTime?)row.ReviewedAt,
            (string?)row.ReviewNote,
            (Guid?)row.DesignatedReviewerId,
            (string?)row.DesignatedReviewerName);
    }

    public async Task<IEnumerable<OvertimeRequestDto>> GetAllAsync()
    {
        const string sql = BaseSql + " ORDER BY o.CreatedAt DESC";
        var rows = (await db.QueryAsync<dynamic>(sql)).ToList();
        var codeMap = await GetProjectCodeMapAsync(rows.Select(r => (string?)r.ProjectIds));
        return rows.Select(r => (OvertimeRequestDto)MapRow(r, codeMap));
    }

    public async Task<PagedResult<OvertimeRequestDto>> GetPagedAsync(int page, int pageSize, Guid userId)
    {
        const string countSql = "SELECT COUNT(*) FROM OvertimeRequests WHERE EmployeeId = @UserId";
        const string sql = BaseSql +
            " WHERE o.EmployeeId = @UserId ORDER BY o.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
        int total = await db.ExecuteScalarAsync<int>(countSql, new { UserId = userId });
        var rows = (await db.QueryAsync<dynamic>(sql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize })).ToList();
        var codeMap = await GetProjectCodeMapAsync(rows.Select(r => (string?)r.ProjectIds));
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<OvertimeRequestDto>(rows.Select(r => (OvertimeRequestDto)MapRow(r, codeMap)), total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<OvertimeRequestDto?> GetByIdAsync(int id)
    {
        const string sql = BaseSql + " WHERE o.Id = @Id";
        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (row is null) return null;
        var codeMap = await GetProjectCodeMapAsync(new[] { (string?)row.ProjectIds });
        return MapRow(row, codeMap);
    }

    public async Task<IEnumerable<OvertimeRequestDto>> GetFilteredAsync(string? status, DateOnly? date, Guid? employeeId)
    {
        // 動態組裝 WHERE 條件，避免字串拼接 — 使用參數化方式傳遞值
        var conditions = new List<string>();

        if (!string.IsNullOrEmpty(status))
            conditions.Add("o.ApprovalStatus = @Status");

        if (date.HasValue)
            conditions.Add("CAST(o.OvertimeDate AS DATE) = @OvertimeDate");

        if (employeeId.HasValue)
            conditions.Add("o.EmployeeId = @EmployeeId");

        var where = conditions.Count > 0
            ? " WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

        var sql = BaseSql + where + " ORDER BY o.OvertimeDate DESC";

        var rows = (await db.QueryAsync<dynamic>(sql, new
        {
            Status       = status,
            OvertimeDate = date.HasValue ? date.Value.ToString("yyyy-MM-dd") : (string?)null,
            EmployeeId   = employeeId,
        })).ToList();

        var codeMap = await GetProjectCodeMapAsync(rows.Select(r => (string?)r.ProjectIds));
        return rows.Select(r => (OvertimeRequestDto)MapRow(r, codeMap));
    }
}
