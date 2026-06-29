using Dapper;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class ApprovalReadService(IDbConnection db) : IApprovalReadService
{
    public async Task<IEnumerable<ApprovalItemDto>> GetAllAsync()
    {
        var rows = await QueryAllRowsAsync();
        return BuildDtos(rows);
    }

    public async Task<ApprovalFlowSummaryDto?> GetActiveByTypeAsync(string applicationType, int? departmentId)
    {
        // 僅讀取「是否含 UseApplicantDesignated 步驟」所需最小欄位；
        // 故意不 JOIN Departments / JobTitles，避免敏感設定外洩給未授權呼叫者。
        // 同一 ApplicationType 可能有多個流程（各部門專屬 + 一個通用預設）。
        // 部門階層繼承：以遞迴 CTE 由呼叫者部門沿 ParentId 往上建立部門鏈（含層距 Depth），
        // 子查詢挑出「呼叫者部門實際會走」的那一筆——優先序＝自身 > 最近祖先 > 通用預設(null)，再取其 steps。
        // 必須與 ApprovalFlowService.ResolveApprovalItemIdAsync 的優先序保持一致。
        const string sql = """
            WITH DeptChain AS (
                SELECT @DepartmentId AS Id, 0 AS Depth
                UNION ALL
                SELECT d.ParentId, dc.Depth + 1
                FROM Departments d
                JOIN DeptChain dc ON d.Id = dc.Id
                WHERE d.ParentId IS NOT NULL
            )
            SELECT ai.Id, ai.ApplicationType,
                   s.StepOrder, s.UseApplicantDesignated, s.DesignatedRequiresDepartment
            FROM ApprovalItems ai
            LEFT JOIN ApprovalSteps s ON ai.Id = s.ApprovalItemId
            WHERE ai.Id = (
                SELECT TOP 1 ai2.Id FROM ApprovalItems ai2
                LEFT JOIN DeptChain dc ON ai2.DepartmentId = dc.Id
                WHERE ai2.ApplicationType = @Type AND ai2.IsActive = 1
                  AND (dc.Id IS NOT NULL OR ai2.DepartmentId IS NULL)
                ORDER BY CASE WHEN ai2.DepartmentId IS NULL THEN 2147483647 ELSE dc.Depth END
            )
            ORDER BY s.StepOrder
            """;

        var rows = (await db.QueryAsync<dynamic>(sql, new { Type = applicationType, DepartmentId = departmentId })).ToList();

        if (rows.Count == 0) return null;

        var first = rows[0];
        var steps = rows
            .Where(r => r.StepOrder is not null)
            .Select(r => new ApprovalFlowStepSummaryDto(
                (int)r.StepOrder,
                (bool)(r.UseApplicantDesignated ?? false),
                (bool)(r.DesignatedRequiresDepartment ?? false)))
            .ToArray();

        return new ApprovalFlowSummaryDto(
            (int)first.Id,
            (string?)first.ApplicationType,
            steps);
    }

    public async Task<ApprovalItemDto?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT ai.Id, ai.Name, ai.Code, ai.Description, ai.IsActive, ai.ApplicationType, ai.DepartmentId, dd.Name AS ItemDepartmentName, ai.CreatedAt,
                   s.Id AS StepId, s.StepOrder, s.DepartmentId AS StepDepartmentId, d.Name AS DepartmentName,
                   s.JobTitleId, j.Name AS JobTitleName,
                   s.UseApplicantDepartment, s.UseDirectSupervisor, s.UseApplicantDesignated, s.DesignatedRequiresDepartment, s.Note
            FROM ApprovalItems ai
            LEFT JOIN Departments dd  ON ai.DepartmentId = dd.Id
            LEFT JOIN ApprovalSteps s ON ai.Id = s.ApprovalItemId
            LEFT JOIN Departments d   ON s.DepartmentId = d.Id
            LEFT JOIN JobTitles j     ON s.JobTitleId = j.Id
            WHERE ai.Id = @Id
            ORDER BY s.StepOrder
            """;

        var rows = await db.QueryAsync<dynamic>(sql, new { Id = id });
        return BuildDtos(rows).FirstOrDefault();
    }

    private async Task<IEnumerable<dynamic>> QueryAllRowsAsync()
    {
        const string sql = """
            SELECT ai.Id, ai.Name, ai.Code, ai.Description, ai.IsActive, ai.ApplicationType, ai.DepartmentId, dd.Name AS ItemDepartmentName, ai.CreatedAt,
                   s.Id AS StepId, s.StepOrder, s.DepartmentId AS StepDepartmentId, d.Name AS DepartmentName,
                   s.JobTitleId, j.Name AS JobTitleName,
                   s.UseApplicantDepartment, s.UseDirectSupervisor, s.UseApplicantDesignated, s.DesignatedRequiresDepartment, s.Note
            FROM ApprovalItems ai
            LEFT JOIN Departments dd  ON ai.DepartmentId = dd.Id
            LEFT JOIN ApprovalSteps s ON ai.Id = s.ApprovalItemId
            LEFT JOIN Departments d   ON s.DepartmentId = d.Id
            LEFT JOIN JobTitles j     ON s.JobTitleId = j.Id
            ORDER BY ai.Id, s.StepOrder
            """;

        return await db.QueryAsync<dynamic>(sql);
    }

    private static IEnumerable<ApprovalItemDto> BuildDtos(IEnumerable<dynamic> rows)
    {
        var dict = new Dictionary<int, (string Name, string Code, string? Desc, bool IsActive, string? AppType, int? DeptId, string? DeptName, DateTime CreatedAt, List<ApprovalStepDto> Steps)>();

        foreach (var row in rows)
        {
            int id = (int)row.Id;
            if (!dict.ContainsKey(id))
                dict[id] = ((string)row.Name, (string)row.Code, (string?)row.Description, (bool)row.IsActive, (string?)row.ApplicationType, (int?)row.DepartmentId, (string?)row.ItemDepartmentName, (DateTime)row.CreatedAt, []);

            if (row.StepId is not null)
            {
                dict[id].Steps.Add(new ApprovalStepDto(
                    (int)row.StepId,
                    (int)row.StepOrder,
                    (int?)row.StepDepartmentId,
                    (string?)row.DepartmentName,
                    (int?)row.JobTitleId,
                    (string?)row.JobTitleName,
                    (bool)row.UseApplicantDepartment,
                    (bool)(row.UseDirectSupervisor ?? false),
                    (bool)(row.UseApplicantDesignated ?? false),
                    (string?)row.Note,
                    (bool)(row.DesignatedRequiresDepartment ?? false)));
            }
        }

        return dict.Select(kv => new ApprovalItemDto(
            kv.Key,
            kv.Value.Name,
            kv.Value.Code,
            kv.Value.Desc,
            kv.Value.IsActive,
            kv.Value.AppType,
            kv.Value.DeptId,
            kv.Value.DeptName,
            kv.Value.Steps.ToArray(),
            kv.Value.CreatedAt));
    }
}
