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

    public async Task<ApprovalFlowSummaryDto?> GetActiveByTypeAsync(string applicationType)
    {
        // 僅讀取「是否含 UseApplicantDesignated 步驟」所需最小欄位；
        // 故意不 JOIN Departments / JobTitles，避免敏感設定外洩給未授權呼叫者。
        // 一個 ApplicationType 至多對應一個啟用流程（CreateAsync/UpdateAsync 已強制唯一），
        // 但可能有多筆 step，故用 QueryAsync 後手動聚合。
        const string sql = """
            SELECT ai.Id, ai.ApplicationType,
                   s.StepOrder, s.UseApplicantDesignated
            FROM ApprovalItems ai
            LEFT JOIN ApprovalSteps s ON ai.Id = s.ApprovalItemId
            WHERE ai.ApplicationType = @Type AND ai.IsActive = 1
            ORDER BY s.StepOrder
            """;

        var rows = (await db.QueryAsync<dynamic>(sql, new { Type = applicationType })).ToList();

        if (rows.Count == 0) return null;

        var first = rows[0];
        var steps = rows
            .Where(r => r.StepOrder is not null)
            .Select(r => new ApprovalFlowStepSummaryDto(
                (int)r.StepOrder,
                (bool)(r.UseApplicantDesignated ?? false)))
            .ToArray();

        return new ApprovalFlowSummaryDto(
            (int)first.Id,
            (string?)first.ApplicationType,
            steps);
    }

    public async Task<ApprovalItemDto?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT ai.Id, ai.Name, ai.Code, ai.Description, ai.IsActive, ai.ApplicationType, ai.CreatedAt,
                   s.Id AS StepId, s.StepOrder, s.DepartmentId, d.Name AS DepartmentName,
                   s.JobTitleId, j.Name AS JobTitleName,
                   s.UseApplicantDepartment, s.UseDirectSupervisor, s.UseApplicantDesignated, s.Note
            FROM ApprovalItems ai
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
            SELECT ai.Id, ai.Name, ai.Code, ai.Description, ai.IsActive, ai.ApplicationType, ai.CreatedAt,
                   s.Id AS StepId, s.StepOrder, s.DepartmentId, d.Name AS DepartmentName,
                   s.JobTitleId, j.Name AS JobTitleName,
                   s.UseApplicantDepartment, s.UseDirectSupervisor, s.UseApplicantDesignated, s.Note
            FROM ApprovalItems ai
            LEFT JOIN ApprovalSteps s ON ai.Id = s.ApprovalItemId
            LEFT JOIN Departments d   ON s.DepartmentId = d.Id
            LEFT JOIN JobTitles j     ON s.JobTitleId = j.Id
            ORDER BY ai.Id, s.StepOrder
            """;

        return await db.QueryAsync<dynamic>(sql);
    }

    private static IEnumerable<ApprovalItemDto> BuildDtos(IEnumerable<dynamic> rows)
    {
        var dict = new Dictionary<int, (string Name, string Code, string? Desc, bool IsActive, string? AppType, DateTime CreatedAt, List<ApprovalStepDto> Steps)>();

        foreach (var row in rows)
        {
            int id = (int)row.Id;
            if (!dict.ContainsKey(id))
                dict[id] = ((string)row.Name, (string)row.Code, (string?)row.Description, (bool)row.IsActive, (string?)row.ApplicationType, (DateTime)row.CreatedAt, []);

            if (row.StepId is not null)
            {
                dict[id].Steps.Add(new ApprovalStepDto(
                    (int)row.StepId,
                    (int)row.StepOrder,
                    (int?)row.DepartmentId,
                    (string?)row.DepartmentName,
                    (int?)row.JobTitleId,
                    (string?)row.JobTitleName,
                    (bool)row.UseApplicantDepartment,
                    (bool)(row.UseDirectSupervisor ?? false),
                    (bool)(row.UseApplicantDesignated ?? false),
                    (string?)row.Note));
            }
        }

        return dict.Select(kv => new ApprovalItemDto(
            kv.Key,
            kv.Value.Name,
            kv.Value.Code,
            kv.Value.Desc,
            kv.Value.IsActive,
            kv.Value.AppType,
            kv.Value.Steps.ToArray(),
            kv.Value.CreatedAt));
    }
}
