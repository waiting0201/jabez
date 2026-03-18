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
