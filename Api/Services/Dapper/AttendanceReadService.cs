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
    ///
    /// 刻意不 JOIN LeaveRequests：請假改由 ListApprovedLeavesInRangeAsync 撈出、
    /// 經 AttendanceLeaveMerger 逐日展開後在 C# 端合併（同日多張假單在 SQL 端 JOIN 會產生重複列）。
    /// </summary>
    private const string ListSql = """
        SELECT a.Id, a.UserId, u.Name AS UserName, a.RecordDate,
               a.ClockInTime, a.ClockInLatitude, a.ClockInLongitude,
               a.ClockOutTime, a.ClockOutLatitude, a.ClockOutLongitude, a.IsClockOutAuto,
               a.OvertimeStartTime, a.OvertimeStartLatitude, a.OvertimeStartLongitude,
               a.OvertimeEndTime, a.OvertimeEndLatitude, a.OvertimeEndLongitude,
               a.OvertimeRequestId, a.CreatedAt, a.IsBusinessTrip, a.Remark
        FROM AttendanceRecords a
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

    public async Task<IReadOnlyList<AttendanceRecordDto>> ListInRangeAsync(
        ProjectAccessScope scope, Guid? employeeId, DateOnly dateFrom, DateOnly dateTo)
    {
        var where = new StringBuilder();
        var parameters = new DynamicParameters();

        where.Append(BuildDeptScopeFilter(scope, parameters));

        if (employeeId.HasValue)
        {
            where.Append(" AND a.UserId = @EmployeeId");
            parameters.Add("EmployeeId", employeeId.Value);
        }

        // a.RecordDate 為 DATE 型別，inclusive 兩端皆可
        where.Append(" AND a.RecordDate >= @DateFrom AND a.RecordDate <= @DateTo");
        parameters.Add("DateFrom", dateFrom.ToDateTime(TimeOnly.MinValue));
        parameters.Add("DateTo",   dateTo.ToDateTime(TimeOnly.MinValue));

        var sql = ListSql + " WHERE 1=1" + where;
        var rows = await db.QueryAsync<dynamic>(sql, parameters);
        return [.. rows.Select(MapListRow)];
    }

    public async Task<IReadOnlyList<AttendanceLeaveSourceRow>> ListApprovedLeavesInRangeAsync(
        ProjectAccessScope scope, Guid? employeeId, DateOnly dateFrom, DateOnly dateTo)
    {
        var where = new StringBuilder();
        var parameters = new DynamicParameters();

        where.Append(BuildDeptScopeFilter(scope, parameters));

        if (employeeId.HasValue)
        {
            where.Append(" AND lr.EmployeeId = @EmployeeId");
            parameters.Add("EmployeeId", employeeId.Value);
        }

        parameters.Add("DateFrom", dateFrom.ToDateTime(TimeOnly.MinValue));
        parameters.Add("DateTo",   dateTo.ToDateTime(TimeOnly.MinValue));

        // 刻意不在此排除銷假：銷假是逐日的，整張單層級過濾會把「部分銷假」的其餘日子一併誤刪。
        // 全數銷完的單其 ApprovalStatus 已被 LeaveRevocationService.ApplyAsync 改為 'cancelled'，
        // 故 approved 過濾天然排除；逐日排除由 ListApprovedRevokedDatesAsync 負責。
        var sql = """
            SELECT lr.Id, lr.EmployeeId AS UserId, u.Name AS UserName,
                   lr.LeaveType, lr.StartDate, lr.EndDate,
                   u.IsShiftWorker
            FROM   LeaveRequests lr
            INNER JOIN Users u ON lr.EmployeeId = u.Id
            WHERE  lr.ApprovalStatus = 'approved'
              AND  CAST(lr.StartDate AS DATE) <= @DateTo
              AND  CAST(lr.EndDate   AS DATE) >= @DateFrom
            """ + where;

        var rows = await db.QueryAsync<AttendanceLeaveSourceRow>(sql, parameters);
        return [.. rows];
    }

    public async Task<IReadOnlyList<LeaveRevokedDateRow>> ListApprovedRevokedDatesAsync(
        IReadOnlyCollection<int> leaveRequestIds)
    {
        // Dapper 展開空清單會產生 "IN ()" 語法錯誤 → 提前短路
        if (leaveRequestIds.Count == 0) return [];

        const string sql = """
            SELECT rv.LeaveRequestId, rvd.Date
            FROM   LeaveRevocationDates rvd
            JOIN   LeaveRevocations rv ON rv.Id = rvd.LeaveRevocationId
            WHERE  rv.ApprovalStatus = 'approved'
              AND  rv.LeaveRequestId IN @LeaveIds
            """;

        var rows = await db.QueryAsync<LeaveRevokedDateRow>(sql, new { LeaveIds = leaveRequestIds });
        return [.. rows];
    }

    public async Task<TodayAttendanceDto?> GetTodayAsync(Guid userId)
    {
        const string sql = """
            SELECT Id, RecordDate,
                   ClockInTime,  ClockInLatitude,  ClockInLongitude,
                   ClockOutTime, ClockOutLatitude, ClockOutLongitude,
                   OvertimeStartTime, OvertimeStartLatitude, OvertimeStartLongitude,
                   OvertimeEndTime,   OvertimeEndLatitude,   OvertimeEndLongitude,
                   OvertimeRequestId, IsBusinessTrip
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
        // 該日已核准銷假 → 放行打卡（共用 LeaveRevocationService.NotRevokedClause，避免規則分岔）
        var sql = $"""
            SELECT TOP 1 lr.Id, lr.LeaveType, lr.StartDate, lr.EndDate
            FROM   LeaveRequests lr
            WHERE  lr.EmployeeId = @UserId
              AND  lr.ApprovalStatus = 'approved'
              AND  lr.StartDate <= @When
              AND  @When < lr.EndDate
              AND  {LeaveRevocationService.NotRevokedClause("lr", "CAST(@When AS DATE)")}
            ORDER BY lr.StartDate ASC
            """;
        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { UserId = userId, When = when });
        return row is null ? null : MapActiveLeaveRow(row);
    }

    public async Task<IReadOnlyList<ActiveLeaveDto>> GetLeavesOnDateAsync(Guid userId, DateOnly date)
    {
        // 該日已核准銷假 → 該日不再算請假（休假日免下班卡的判定同步排除）
        var sql = $"""
            SELECT lr.Id, lr.LeaveType, lr.StartDate, lr.EndDate
            FROM   LeaveRequests lr
            WHERE  lr.EmployeeId = @UserId
              AND  lr.ApprovalStatus = 'approved'
              AND  lr.StartDate <  @NextDay
              AND  lr.EndDate   >  @DayStart
              AND  {LeaveRevocationService.NotRevokedClause("lr", "CAST(@DayStart AS DATE)")}
            ORDER BY lr.StartDate ASC
            """;
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var nextDay  = dayStart.AddDays(1);
        var rows = await db.QueryAsync<dynamic>(sql, new { UserId = userId, DayStart = dayStart, NextDay = nextDay });
        return rows.Select(MapActiveLeaveRow).ToList();
    }

    /// <summary>打卡列 → DTO。請假欄位一律留 null，由 AttendanceLeaveMerger 事後以 with { } 補上。</summary>
    private static AttendanceRecordDto MapListRow(dynamic row) =>
        new(
            (int?)row.Id,
            (Guid)row.UserId,
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
            (DateTime?)row.CreatedAt,
            null,   // LeaveType
            null,   // LeaveStartDate
            null,   // LeaveEndDate
            // 具名參數跳過 LeaveHours / Leaves（由 AttendanceLeaveMerger 事後以 with { } 補上）
            IsBusinessTrip: (bool)row.IsBusinessTrip,
            Remark:         (string?)row.Remark);

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
            Array.Empty<ActiveLeaveDto>(),
            IsBusinessTrip: (bool)row.IsBusinessTrip);

    private static ActiveLeaveDto MapActiveLeaveRow(dynamic row) =>
        new(
            (int)row.Id,
            (string)row.LeaveType,
            (DateTime)row.StartDate,
            (DateTime)row.EndDate);
}
