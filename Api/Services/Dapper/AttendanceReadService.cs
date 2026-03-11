using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;
using System.Text;

namespace Jabez.Api.Services.Dapper;

public sealed class AttendanceReadService(IDbConnection db) : IAttendanceReadService
{
    private const string ListSql = """
        SELECT a.Id, u.Name AS UserName, a.RecordDate,
               a.ClockInTime, a.ClockOutTime,
               a.OvertimeStartTime, a.OvertimeEndTime,
               a.OvertimeRequestId, a.CreatedAt,
               lr.LeaveType, lr.StartDate AS LeaveStartDate, lr.EndDate AS LeaveEndDate
        FROM AttendanceRecords a
        LEFT JOIN Users u ON a.UserId = u.Id
        LEFT JOIN LeaveRequests lr ON a.UserId = lr.EmployeeId
            AND CAST(a.RecordDate AS DATE) >= CAST(lr.StartDate AS DATE)
            AND CAST(a.RecordDate AS DATE) <= CAST(lr.EndDate AS DATE)
            AND lr.ApprovalStatus = 'approved'
        """;

    public async Task<PagedResult<AttendanceRecordDto>> GetPagedAsync(int page, int pageSize,
        Guid? employeeId = null, int? year = null, int? month = null)
    {
        var where = new StringBuilder();
        var parameters = new DynamicParameters();
        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        if (employeeId.HasValue)
        {
            where.Append(" AND a.UserId = @EmployeeId");
            parameters.Add("EmployeeId", employeeId.Value);
        }

        if (year.HasValue)
        {
            where.Append(" AND YEAR(a.RecordDate) = @Year");
            parameters.Add("Year", year.Value);
        }

        if (month.HasValue)
        {
            where.Append(" AND MONTH(a.RecordDate) = @Month");
            parameters.Add("Month", month.Value);
        }

        var whereClause = where.Length > 0 ? " WHERE 1=1" + where : "";

        var countSql = "SELECT COUNT(*) FROM AttendanceRecords a" + whereClause;
        var sql = ListSql + whereClause +
            " ORDER BY a.RecordDate DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        int total = await db.ExecuteScalarAsync<int>(countSql, parameters);
        var rows = await db.QueryAsync<dynamic>(sql, parameters);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<AttendanceRecordDto>(rows.Select(MapListRow), total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<TodayAttendanceDto?> GetTodayAsync(Guid userId)
    {
        const string sql = """
            SELECT Id, RecordDate,
                   ClockInTime,  ClockInLatitude,  ClockInLongitude,
                   ClockOutTime, ClockOutLatitude, ClockOutLongitude,
                   OvertimeStartTime, OvertimeStartLatitude, OvertimeStartLongitude,
                   OvertimeEndTime,   OvertimeEndLatitude,   OvertimeEndLongitude,
                   OvertimeRequestId
            FROM AttendanceRecords
            WHERE UserId = @UserId AND RecordDate = @Today
            """;
        var taipeiNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"));
        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { UserId = userId, Today = taipeiNow.Date });
        return row is null ? null : MapTodayRow(row);
    }

    private static AttendanceRecordDto MapListRow(dynamic row) =>
        new(
            (int)row.Id,
            (string?)row.UserName ?? "—",
            (DateTime)row.RecordDate,
            (DateTime?)row.ClockInTime,
            (DateTime?)row.ClockOutTime,
            (DateTime?)row.OvertimeStartTime,
            (DateTime?)row.OvertimeEndTime,
            (int?)row.OvertimeRequestId,
            (DateTime)row.CreatedAt,
            (string?)row.LeaveType,
            (DateTime?)row.LeaveStartDate,
            (DateTime?)row.LeaveEndDate);

    private static TodayAttendanceDto MapTodayRow(dynamic row) =>
        new(
            (int)row.Id,
            (DateTime)row.RecordDate,
            (DateTime?)row.ClockInTime,
            (double?)row.ClockInLatitude,
            (double?)row.ClockInLongitude,
            (DateTime?)row.ClockOutTime,
            (double?)row.ClockOutLatitude,
            (double?)row.ClockOutLongitude,
            (DateTime?)row.OvertimeStartTime,
            (double?)row.OvertimeStartLatitude,
            (double?)row.OvertimeStartLongitude,
            (DateTime?)row.OvertimeEndTime,
            (double?)row.OvertimeEndLatitude,
            (double?)row.OvertimeEndLongitude,
            (int?)row.OvertimeRequestId);
}
