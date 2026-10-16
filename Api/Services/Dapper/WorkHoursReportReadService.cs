using Dapper;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using System.Data;

namespace Jabez.Api.Services.Dapper;

/// <summary>
/// 工時總表原料查詢。只管查：乘 8 小時、依 (員工, 專案) 彙總、排序都在 WorkHoursReportHandler。
/// 母體＝區間內 <c>ClockOutTime IS NOT NULL</c> 的打卡紀錄（加班不併入，只看正常上下班）。
/// </summary>
public sealed class WorkHoursReportReadService(IDbConnection db) : IWorkHoursReportReadService
{
    public async Task<IReadOnlyList<WorkHoursRaw>> GetRowsAsync(
        ProjectAccessScope scope, DateTime dateFrom, DateTime dateTo, int? departmentId, int? projectId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("DateFrom", dateFrom.Date);
        parameters.Add("DateTo", dateTo.Date);   // RecordDate 為 DATE，兩端皆含

        var filter = "";
        if (!scope.SeeAll)
        {
            if (scope.AllowedDepartmentIds.Count == 0) filter += " AND 1=0";
            else
            {
                parameters.Add("AllowedDeptIds", scope.AllowedDepartmentIds);
                filter += " AND u.DepartmentId IN @AllowedDeptIds";
            }
        }
        if (departmentId.HasValue)
        {
            parameters.Add("DepartmentId", departmentId.Value);
            filter += " AND u.DepartmentId = @DepartmentId";
        }
        if (projectId.HasValue)
        {
            // 以 al.ProjectId 篩選：LEFT JOIN 出來的「無分配」列 al.ProjectId 為 NULL 會被一併排除
            parameters.Add("ProjectId", projectId.Value);
            filter += " AND al.ProjectId = @ProjectId";
        }

        var sql = $"""
            SELECT a.RecordDate AS Date, d.Name AS DepartmentName,
                   u.Id AS EmployeeId, u.Name AS EmployeeName,
                   p.Id AS ProjectId, p.Code AS ProjectCode, p.Name AS ProjectName,
                   al.Percentage AS Percentage, a.IsClockOutAuto AS IsAutoClockOut
            FROM AttendanceRecords a
            INNER JOIN Users u ON u.Id = a.UserId
            LEFT JOIN Departments d ON d.Id = u.DepartmentId
            LEFT JOIN AttendanceProjectAllocations al ON al.AttendanceRecordId = a.Id
            LEFT JOIN Projects p ON p.Id = al.ProjectId
            WHERE a.ClockOutTime IS NOT NULL
              AND a.RecordDate >= @DateFrom AND a.RecordDate <= @DateTo
              AND u.IsSuperAdmin = 0{filter}
            ORDER BY a.RecordDate, u.Name, u.Id, al.SortOrder
            """;

        return [.. await db.QueryAsync<WorkHoursRaw>(sql, parameters)];
    }
}
