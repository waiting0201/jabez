using Dapper;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using System.Data;

namespace Jabez.Api.Services.Dapper;

/// <summary>
/// 加班補休時數總表的原料查詢。只管查，日別判定 / 級距切分 / 餘額公式 / 金額遮蔽都在 CompensatoryReportHandler。
/// 補休帳的篩選條件必須與 LeaveRequestHandler.ComputeCompensatoryAsync 一致：
///   取得 = 已核准 ∧ CompensationType = 'compensatory'（選加班費的單已轉現金，不得進補休池）
///   已用 = 補休假 ∧ pending / approved
/// </summary>
public sealed class CompensatoryReportReadService(IDbConnection db) : ICompensatoryReportReadService
{
    private static string BuildDeptScopeFilter(ProjectAccessScope scope, DynamicParameters parameters)
    {
        if (scope.SeeAll) return "";
        if (scope.AllowedDepartmentIds.Count == 0) return " AND 1=0";
        parameters.Add("AllowedDeptIds", scope.AllowedDepartmentIds);
        return " AND u.DepartmentId IN @AllowedDeptIds";
    }

    public async Task<IReadOnlyList<CompensatoryEmployeeRaw>> GetEmployeesAsync(
        ProjectAccessScope scope, DateTime dateFrom, DateTime dateTo)
    {
        var parameters = new DynamicParameters();
        parameters.Add("DateFrom", dateFrom.Date);
        parameters.Add("DateToExclusive", dateTo.Date.AddDays(1));

        var sql = $"""
            SELECT u.Id AS EmployeeId, u.Name AS EmployeeName, u.DepartmentId, d.Name AS DepartmentName,
                   u.IsShiftWorker, u.BaseSalary,
                   ISNULL(u.CompensatoryOpeningHours, 0) AS OpeningHours,
                   ISNULL((SELECT SUM(ISNULL(o.SettledHours, o.EstimatedHours)) FROM OvertimeRequests o
                           WHERE o.EmployeeId = u.Id AND o.ApprovalStatus = 'approved'
                             AND o.CompensationType = 'compensatory'), 0) AS EarnedHours,
                   ISNULL((SELECT SUM(l.Hours) FROM LeaveRequests l
                           WHERE l.EmployeeId = u.Id AND l.LeaveType = 'compensatory'
                             AND l.ApprovalStatus IN ('approved', 'pending')), 0) AS UsedHours,
                   ISNULL((SELECT SUM(l.Hours) FROM LeaveRequests l
                           WHERE l.EmployeeId = u.Id AND l.LeaveType = 'compensatory'
                             AND l.ApprovalStatus IN ('approved', 'pending')
                             AND l.StartDate >= @DateFrom AND l.StartDate < @DateToExclusive), 0) AS PeriodUsedHours
            FROM Users u
            LEFT JOIN Departments d ON d.Id = u.DepartmentId
            WHERE u.IsSuperAdmin = 0{BuildDeptScopeFilter(scope, parameters)}
            ORDER BY d.Name, u.Name
            """;

        return [.. await db.QueryAsync<CompensatoryEmployeeRaw>(sql, parameters)];
    }

    public async Task<IReadOnlyList<CompensatoryOvertimeRaw>> GetPeriodOvertimesAsync(
        ProjectAccessScope scope, DateTime dateFrom, DateTime dateTo)
    {
        var parameters = new DynamicParameters();
        parameters.Add("DateFrom", dateFrom.Date);
        parameters.Add("DateTo", dateTo.Date);

        // o.OvertimeDate 為 DATE 型別，inclusive 兩端皆可（比照 OvertimeReportReadService）
        var sql = $"""
            SELECT o.EmployeeId, o.OvertimeDate,
                   ISNULL(o.SettledHours, o.EstimatedHours) AS BillableHours   -- 給付基準（2026-10 防灌工時）
            FROM OvertimeRequests o
            INNER JOIN Users u ON o.EmployeeId = u.Id
            WHERE o.ApprovalStatus = 'approved'
              AND o.CompensationType = 'compensatory'
              AND o.OvertimeDate >= @DateFrom AND o.OvertimeDate <= @DateTo
              AND u.IsSuperAdmin = 0{BuildDeptScopeFilter(scope, parameters)}
            """;

        return [.. await db.QueryAsync<CompensatoryOvertimeRaw>(sql, parameters)];
    }
}
