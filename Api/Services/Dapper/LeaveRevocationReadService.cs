using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class LeaveRevocationReadService(IDbConnection db) : ILeaveRevocationReadService
{
    private const string BaseSql = """
        SELECT rv.Id, rv.LeaveRequestId, u.Name AS EmployeeName,
               rv.Reason, rv.RevokedHours, rv.ApprovalStatus, rv.CreatedAt,
               rv.ReviewedAt, rv.ReviewNote, rv.ApprovalItemId, rv.CurrentStepOrder, rv.ReviewedById,
               lr.LeaveType, lr.StartDate AS LeaveStartDate, lr.EndDate AS LeaveEndDate,
               lr.Hours AS LeaveHours, lr.OriginalHours AS LeaveOriginalHours,
               lr.ApprovalStatus AS LeaveApprovalStatus
        FROM LeaveRevocations rv
        JOIN LeaveRequests lr ON rv.LeaveRequestId = lr.Id
        LEFT JOIN Users u ON rv.EmployeeId = u.Id
        """;

    public async Task<PagedResult<LeaveRevocationDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null)
    {
        var countSql = "SELECT COUNT(*) FROM LeaveRevocations" + (userId.HasValue ? " WHERE EmployeeId = @UserId" : "");
        var sql = BaseSql +
            (userId.HasValue ? " WHERE rv.EmployeeId = @UserId" : "") +
            " ORDER BY rv.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        int total = await db.ExecuteScalarAsync<int>(countSql, new { UserId = userId });
        var rows = await db.QueryAsync<dynamic>(sql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize });
        List<LeaveRevocationDto> items = [];
        foreach (var row in rows)
        {
            LeaveRevocationDto dto = MapRow(row, null);
            items.Add(dto);
        }

        // 逐日明細一次撈回（列表也需要顯示「取消 N 天」的日期）
        if (items.Count > 0)
        {
            var byRevocation = await GetDatesAsync([.. items.Select(i => i.Id)]);
            items = [.. items.Select(i => i with
            {
                Dates = byRevocation.TryGetValue(i.Id, out var ds) ? ds : []
            })];
        }

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<LeaveRevocationDto>(items, total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<LeaveRevocationDto?> GetByIdAsync(int id)
    {
        const string sql = BaseSql + " WHERE rv.Id = @Id";
        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (row is null) return null;

        var dates = (await GetDatesAsync([id])).TryGetValue(id, out var ds) ? ds : [];

        // 指定審核者（GetByIdAsync 才需要，列表查詢不包含）
        const string drSql = """
            SELECT rdr.Id, rdr.ReviewerId, u.Name AS ReviewerName,
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            WHERE rdr.RequestType = 'leave_revocation' AND rdr.RequestId = @RequestId
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

        LeaveRevocationDto dto = MapRow(row, dates);
        return dto with { DesignatedReviewers = designatedReviewers.Length > 0 ? designatedReviewers : null };
    }

    private async Task<Dictionary<int, List<LeaveRevocationDateDto>>> GetDatesAsync(int[] revocationIds)
    {
        const string sql = """
            SELECT LeaveRevocationId, Date, Hours
            FROM LeaveRevocationDates
            WHERE LeaveRevocationId IN @Ids
            ORDER BY Date
            """;
        var rows = await db.QueryAsync<dynamic>(sql, new { Ids = revocationIds });
        return rows
            .GroupBy(r => (int)r.LeaveRevocationId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new LeaveRevocationDateDto((DateTime)r.Date, (decimal)r.Hours)).ToList());
    }

    private static LeaveRevocationDto MapRow(dynamic row, List<LeaveRevocationDateDto>? dates) => new(
        (int)row.Id,
        (int)row.LeaveRequestId,
        (string?)row.EmployeeName ?? "—",
        (string)row.Reason,
        (decimal)row.RevokedHours,
        (string)row.ApprovalStatus,
        (DateTime)row.CreatedAt,
        (DateTime?)row.ReviewedAt,
        (string?)row.ReviewNote,
        ApprovalItemId:      (int?)row.ApprovalItemId,
        CurrentStepOrder:    (int?)row.CurrentStepOrder,
        ReviewedById:        (Guid?)row.ReviewedById,
        Dates:               dates,
        LeaveType:           (string)row.LeaveType,
        LeaveStartDate:      (DateTime)row.LeaveStartDate,
        LeaveEndDate:        (DateTime)row.LeaveEndDate,
        LeaveHours:          (decimal)row.LeaveHours,
        LeaveOriginalHours:  (decimal?)row.LeaveOriginalHours,
        LeaveApprovalStatus: (string)row.LeaveApprovalStatus);
}
