using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using System.Data;
using System.Text;

namespace Jabez.Api.Services.Dapper;

public sealed class AttendanceReadService(IDbConnection db) : IAttendanceReadService
{
    /// <summary>
    /// 列表 SQL：以 INNER JOIN Users 確保部門 scope 過濾穩定（離職員工 / 已刪除使用者不會出現）。
    /// 部門 scope 透過 BuildDeptScopeFilter 加入 WHERE 鏈。
    /// </summary>
    private const string ListSql = """
        SELECT a.Id, u.Name AS UserName, a.RecordDate,
               a.ClockInTime, a.ClockInLatitude, a.ClockInLongitude,
               a.ClockOutTime, a.ClockOutLatitude, a.ClockOutLongitude, a.IsClockOutAuto,
               a.OvertimeStartTime, a.OvertimeStartLatitude, a.OvertimeStartLongitude,
               a.OvertimeEndTime, a.OvertimeEndLatitude, a.OvertimeEndLongitude,
               a.OvertimeRequestId, a.CreatedAt,
               lr.LeaveType, lr.StartDate AS LeaveStartDate, lr.EndDate AS LeaveEndDate
        FROM AttendanceRecords a
        INNER JOIN Users u ON a.UserId = u.Id
        LEFT JOIN LeaveRequests lr ON a.UserId = lr.EmployeeId
            AND CAST(a.RecordDate AS DATE) >= CAST(lr.StartDate AS DATE)
            AND CAST(a.RecordDate AS DATE) <= CAST(lr.EndDate AS DATE)
            AND lr.ApprovalStatus = 'approved'
            -- 該日已核准銷假 → 不再視為請假日（部分銷假可挖空中間日）
            AND NOT EXISTS (
                SELECT 1 FROM LeaveRevocationDates rvd
                JOIN LeaveRevocations rv ON rv.Id = rvd.LeaveRevocationId
                WHERE rv.LeaveRequestId = lr.Id
                  AND rv.ApprovalStatus = 'approved'
                  AND rvd.Date = CAST(a.RecordDate AS DATE))
        """;

    private const string CountFromSql = """
        SELECT COUNT(*) FROM AttendanceRecords a
        INNER JOIN Users u ON a.UserId = u.Id
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

    public async Task<PagedResult<AttendanceRecordDto>> GetPagedAsync(ProjectAccessScope scope, int page, int pageSize,
        Guid? employeeId = null, DateOnly? dateFrom = null, DateOnly? dateTo = null)
    {
        var where = new StringBuilder();
        var parameters = new DynamicParameters();
        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        where.Append(BuildDeptScopeFilter(scope, parameters));

        if (employeeId.HasValue)
        {
            where.Append(" AND a.UserId = @EmployeeId");
            parameters.Add("EmployeeId", employeeId.Value);
        }

        // a.RecordDate 為 DATE 型別，inclusive 兩端皆可
        if (dateFrom.HasValue)
        {
            where.Append(" AND a.RecordDate >= @DateFrom");
            parameters.Add("DateFrom", dateFrom.Value.ToDateTime(TimeOnly.MinValue));
        }

        if (dateTo.HasValue)
        {
            where.Append(" AND a.RecordDate <= @DateTo");
            parameters.Add("DateTo", dateTo.Value.ToDateTime(TimeOnly.MinValue));
        }

        var whereClause = where.Length > 0 ? " WHERE 1=1" + where : "";

        var countSql = CountFromSql + whereClause;
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

    public async Task<ActiveLeaveDto?> GetActiveLeaveAtAsync(Guid userId, DateTime when)
    {
        const string sql = """
            SELECT TOP 1 lr.Id, lr.LeaveType, lr.StartDate, lr.EndDate
            FROM   LeaveRequests lr
            WHERE  lr.EmployeeId = @UserId
              AND  lr.ApprovalStatus = 'approved'
              AND  lr.StartDate <= @When
              AND  @When < lr.EndDate
              -- 該日已核准銷假 → 放行打卡
              AND  NOT EXISTS (
                    SELECT 1 FROM LeaveRevocationDates rvd
                    JOIN LeaveRevocations rv ON rv.Id = rvd.LeaveRevocationId
                    WHERE rv.LeaveRequestId = lr.Id
                      AND rv.ApprovalStatus = 'approved'
                      AND rvd.Date = CAST(@When AS DATE))
            ORDER BY lr.StartDate ASC
            """;
        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { UserId = userId, When = when });
        return row is null ? null : MapActiveLeaveRow(row);
    }

    public async Task<IReadOnlyList<ActiveLeaveDto>> GetLeavesOnDateAsync(Guid userId, DateOnly date)
    {
        const string sql = """
            SELECT lr.Id, lr.LeaveType, lr.StartDate, lr.EndDate
            FROM   LeaveRequests lr
            WHERE  lr.EmployeeId = @UserId
              AND  lr.ApprovalStatus = 'approved'
              AND  lr.StartDate <  @NextDay
              AND  lr.EndDate   >  @DayStart
              -- 該日已核准銷假 → 該日不再算請假（休假日免下班卡的判定同步排除）
              AND  NOT EXISTS (
                    SELECT 1 FROM LeaveRevocationDates rvd
                    JOIN LeaveRevocations rv ON rv.Id = rvd.LeaveRevocationId
                    WHERE rv.LeaveRequestId = lr.Id
                      AND rv.ApprovalStatus = 'approved'
                      AND rvd.Date = CAST(@DayStart AS DATE))
            ORDER BY lr.StartDate ASC
            """;
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var nextDay  = dayStart.AddDays(1);
        var rows = await db.QueryAsync<dynamic>(sql, new { UserId = userId, DayStart = dayStart, NextDay = nextDay });
        return rows.Select(MapActiveLeaveRow).ToList();
    }

    private static AttendanceRecordDto MapListRow(dynamic row) =>
        new(
            (int)row.Id,
            (string?)row.UserName ?? "—",
            (DateTime)row.RecordDate,
            (DateTime?)row.ClockInTime,
            (double?)row.ClockInLatitude,
            (double?)row.ClockInLongitude,
            (DateTime?)row.ClockOutTime,
            (double?)row.ClockOutLatitude,
            (double?)row.ClockOutLongitude,
            (bool)row.IsClockOutAuto,
            (DateTime?)row.OvertimeStartTime,
            (double?)row.OvertimeStartLatitude,
            (double?)row.OvertimeStartLongitude,
            (DateTime?)row.OvertimeEndTime,
            (double?)row.OvertimeEndLatitude,
            (double?)row.OvertimeEndLongitude,
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
            (int?)row.OvertimeRequestId,
            Array.Empty<ActiveLeaveDto>());

    private static ActiveLeaveDto MapActiveLeaveRow(dynamic row) =>
        new(
            (int)row.Id,
            (string)row.LeaveType,
            (DateTime)row.StartDate,
            (DateTime)row.EndDate);
}
