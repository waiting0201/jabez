using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class OvertimeRequestReadService(IDbConnection db) : IOvertimeRequestReadService
{
    private const string BaseSql = """
        SELECT o.Id, u.Name AS EmployeeName,
               o.OvertimeDate,
               o.EstimatedHours, o.Reason,
               o.ApprovalStatus, o.CreatedAt, o.ReviewedAt, o.ReviewNote,
               o.ApprovalItemId, o.CurrentStepOrder, o.ReviewedById,
               o.CompensationType, o.OvertimePayAmount, o.HourlyRateSnapshot,
               o.PayableHours, o.IsHolidayOvertime
        FROM OvertimeRequests o
        LEFT JOIN Users u ON o.EmployeeId = u.Id
        """;

    /// <summary>
    /// 批次載入一批加班單的關聯專案明細（含 Code / Name），依 SortOrder 排序。
    /// OvertimeReportReadService 共用同一份，避免兩邊實作漂移。
    /// </summary>
    public static async Task<Dictionary<int, OvertimeProjectDto[]>> LoadProjectsAsync(IDbConnection db, IEnumerable<int> requestIds)
    {
        var ids = requestIds.Distinct().ToArray();
        if (ids.Length == 0) return new();

        const string sql = """
            SELECT orp.OvertimeRequestId, orp.ProjectId,
                   p.Code AS ProjectCode, p.Name AS ProjectName, orp.EstimatedHours
            FROM OvertimeRequestProjects orp
            JOIN Projects p ON orp.ProjectId = p.Id
            WHERE orp.OvertimeRequestId IN @Ids
            ORDER BY orp.OvertimeRequestId, orp.SortOrder
            """;
        var rows = await db.QueryAsync<dynamic>(sql, new { Ids = ids });
        return rows
            .GroupBy(r => (int)r.OvertimeRequestId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new OvertimeProjectDto(
                        (int)r.ProjectId,
                        (string)r.ProjectCode,
                        (string)r.ProjectName,
                        (decimal)r.EstimatedHours)).ToArray());
    }

    private static OvertimeRequestDto MapRow(dynamic row, Dictionary<int, OvertimeProjectDto[]> projectMap)
    {
        return new(
            (int)row.Id,
            (string?)row.EmployeeName ?? "—",
            (DateTime)row.OvertimeDate,
            projectMap.GetValueOrDefault((int)row.Id, []),
            (decimal)row.EstimatedHours,
            (string)row.Reason,
            (string)row.ApprovalStatus,
            (DateTime)row.CreatedAt,
            (DateTime?)row.ReviewedAt,
            (string?)row.ReviewNote,
            ApprovalItemId:     (int?)row.ApprovalItemId,
            CurrentStepOrder:   (int?)row.CurrentStepOrder,
            ReviewedById:       (Guid?)row.ReviewedById,
            CompensationType:   (string?)row.CompensationType ?? "compensatory",
            OvertimePayAmount:  (decimal?)row.OvertimePayAmount,
            HourlyRateSnapshot: (decimal?)row.HourlyRateSnapshot,
            PayableHours:       (decimal?)row.PayableHours,
            IsHolidayOvertime:  (bool?)row.IsHolidayOvertime);
    }

    public async Task<IEnumerable<OvertimeRequestDto>> GetAllAsync()
    {
        const string sql = BaseSql + " ORDER BY o.CreatedAt DESC";
        var rows = (await db.QueryAsync<dynamic>(sql)).ToList();
        var projectMap = await LoadProjectsAsync(db, rows.Select(r => (int)r.Id));
        return rows.Select(r => (OvertimeRequestDto)MapRow(r, projectMap));
    }

    public async Task<PagedResult<OvertimeRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null)
    {
        var userFilter = userId.HasValue ? "WHERE EmployeeId = @UserId" : "";
        var countSql = $"SELECT COUNT(*) FROM OvertimeRequests {userFilter}";
        var sql = BaseSql +
            (userId.HasValue ? " WHERE o.EmployeeId = @UserId" : "") +
            " ORDER BY o.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
        int total = await db.ExecuteScalarAsync<int>(countSql, new { UserId = userId });
        var rows = (await db.QueryAsync<dynamic>(sql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize })).ToList();
        var projectMap = await LoadProjectsAsync(db, rows.Select(r => (int)r.Id));
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<OvertimeRequestDto>(rows.Select(r => (OvertimeRequestDto)MapRow(r, projectMap)), total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<OvertimeRequestDto?> GetByIdAsync(int id)
    {
        const string sql = BaseSql + " WHERE o.Id = @Id";
        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (row is null) return null;

        var projectMap = await LoadProjectsAsync(db, [(int)row.Id]);

        // 額外查詢指定審核者（GetByIdAsync 才需要，列表查詢不包含）
        const string drSql = """
            SELECT rdr.Id, rdr.ReviewerId, u.Name AS ReviewerName,
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            WHERE rdr.RequestType = 'overtime' AND rdr.RequestId = @RequestId
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

        OvertimeRequestDto dto = MapRow(row, projectMap);
        return dto with { DesignatedReviewers = designatedReviewers.Length > 0 ? designatedReviewers : null };
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

        var projectMap = await LoadProjectsAsync(db, rows.Select(r => (int)r.Id));
        return rows.Select(r => (OvertimeRequestDto)MapRow(r, projectMap));
    }
}
