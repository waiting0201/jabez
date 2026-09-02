using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class LeaveRequestReadService(IDbConnection db) : ILeaveRequestReadService
{
    private const string BaseSql = """
        SELECT lr.Id, u.Name AS EmployeeName,
               lr.LeaveType, lr.StartDate, lr.EndDate, lr.Hours, lr.OriginalHours, lr.Reason,
               lr.ApprovalStatus, lr.CreatedAt, lr.SubmittedAt, lr.ReviewedAt, lr.ReviewNote,
               lr.ApprovalItemId, lr.CurrentStepOrder, lr.ReviewedById,
               lr.BereavementRelationship, lr.AgentUserId, ag.Name AS AgentName,
               lr.ChildBirthDate, lr.ContinueInsurance
        FROM LeaveRequests lr
        LEFT JOIN Users u  ON lr.EmployeeId  = u.Id
        LEFT JOIN Users ag ON lr.AgentUserId = ag.Id
        """;

    public async Task<IEnumerable<LeaveRequestDto>> GetAllAsync()
    {
        const string sql = BaseSql + " ORDER BY COALESCE(lr.SubmittedAt, lr.CreatedAt) DESC";
        var rows = await db.QueryAsync<dynamic>(sql);
        return rows.Select(MapRow);
    }

    public async Task<PagedResult<LeaveRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null)
    {
        var userFilter = userId.HasValue ? "WHERE EmployeeId = @UserId" : "";
        var countSql = $"SELECT COUNT(*) FROM LeaveRequests {userFilter}";
        var sql = BaseSql +
            (userId.HasValue ? " WHERE lr.EmployeeId = @UserId" : "") +
            " ORDER BY COALESCE(lr.SubmittedAt, lr.CreatedAt) DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
        int total = await db.ExecuteScalarAsync<int>(countSql, new { UserId = userId });
        var rows = await db.QueryAsync<dynamic>(sql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize });
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<LeaveRequestDto>(rows.Select(MapRow), total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<LeaveRequestDto?> GetByIdAsync(int id)
    {
        const string sql = BaseSql + " WHERE lr.Id = @Id";
        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (row is null) return null;

        // 額外查詢指定審核者（GetByIdAsync 才需要，列表查詢不包含）
        const string drSql = """
            SELECT rdr.Id, rdr.ReviewerId, u.Name AS ReviewerName,
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            WHERE rdr.RequestType = 'leave' AND rdr.RequestId = @RequestId
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

        LeaveRequestDto dto = MapRow(row);
        return dto with { DesignatedReviewers = designatedReviewers.Length > 0 ? designatedReviewers : null };
    }

    public async Task<IEnumerable<OverlappingLeaveRequestDto>> GetOverlappingRequestsAsync(
        Guid employeeId, DateTime startDate, DateTime endDate, int? excludeId = null)
    {
        // 半開區間嚴格相交：existing.Start < new.End AND existing.End > new.Start
        // 半天/小時假時段已編碼於 datetime；同日 09–12 + 14–17 不視為重疊
        const string sql = """
            SELECT lr.Id, lr.LeaveType, lr.StartDate, lr.EndDate, lr.ApprovalStatus, lr.Hours
            FROM LeaveRequests lr
            WHERE lr.EmployeeId = @EmployeeId
              AND lr.ApprovalStatus IN ('draft','pending','approved')
              AND lr.StartDate < @EndDate
              AND lr.EndDate   > @StartDate
              AND (@ExcludeId IS NULL OR lr.Id <> @ExcludeId)
            ORDER BY lr.StartDate
            """;
        return await db.QueryAsync<OverlappingLeaveRequestDto>(sql, new
        {
            EmployeeId = employeeId,
            StartDate  = startDate,
            EndDate    = endDate,
            ExcludeId  = excludeId.HasValue ? (object)excludeId.Value : DBNull.Value,
        });
    }

    private static LeaveRequestDto MapRow(dynamic row)
    {
        var leaveType = (string)row.LeaveType;
        return new(
            (int)row.Id,
            (string?)row.EmployeeName ?? "—",
            leaveType,
            (DateTime)row.StartDate,
            (DateTime)row.EndDate,
            (decimal)row.Hours,
            (string)row.Reason,
            (string)row.ApprovalStatus,
            (DateTime)row.CreatedAt,
            (DateTime?)row.SubmittedAt,
            (DateTime?)row.ReviewedAt,
            (string?)row.ReviewNote,
            ApprovalItemId:          (int?)row.ApprovalItemId,
            CurrentStepOrder:        (int?)row.CurrentStepOrder,
            ReviewedById:            (Guid?)row.ReviewedById,
            BereavementRelationship: (string?)row.BereavementRelationship,
            TimeUnit:                GetTimeUnitString(leaveType),
            AgentUserId:             (Guid?)row.AgentUserId,
            AgentName:               (string?)row.AgentName,
            OriginalHours:           (decimal?)row.OriginalHours,
            ChildBirthDate:          (DateTime?)row.ChildBirthDate,
            ContinueInsurance:       (bool?)row.ContinueInsurance);
    }

    /// <summary>依假別取得時間單位字串（與 LeaveRequestHandler 保持一致）</summary>
    private static string GetTimeUnitString(string leaveType) => leaveType switch
    {
        "personal" or "sick" or "prenatal_checkup" or "paternity"
            or "family_care"                                        => "hour",
        "annual" or "compensatory" or "senior_executive"            => "half_day",
        _                                                            => "day",
    };
}
