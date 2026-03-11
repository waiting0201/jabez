using Dapper;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class InsuranceBracketReadService(IDbConnection db) : IInsuranceBracketReadService
{
    private const string SelectColumns = """
        Id, SalaryBracket, LaborInsuranceEmployee,
        HealthInsuranceEmployee, CreatedAt
        """;

    public async Task<IEnumerable<InsuranceBracketDto>> GetAllAsync()
    {
        var sql = $"SELECT {SelectColumns} FROM InsuranceBrackets ORDER BY SalaryBracket ASC";

        var rows = await db.QueryAsync<dynamic>(sql);
        return rows.Select(MapToDto);
    }

    public async Task<InsuranceBracketDto?> GetByIdAsync(int id)
    {
        var sql = $"SELECT {SelectColumns} FROM InsuranceBrackets WHERE Id = @Id";

        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToDto(row);
    }

    /// <summary>根據薪資查詢對應級距（向上取最近級距；超過最高則回傳最高）</summary>
    public async Task<InsuranceBracketDto?> GetBySalaryAsync(decimal salary)
    {
        var sql = $"""
            SELECT TOP 1 {SelectColumns}
            FROM InsuranceBrackets
            WHERE SalaryBracket >= @Salary
            ORDER BY SalaryBracket ASC
            """;

        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Salary = salary });

        if (row is null)
        {
            var fallbackSql = $"""
                SELECT TOP 1 {SelectColumns}
                FROM InsuranceBrackets
                ORDER BY SalaryBracket DESC
                """;
            row = await db.QueryFirstOrDefaultAsync<dynamic>(fallbackSql);
        }

        return row is null ? null : MapToDto(row);
    }

    private static InsuranceBracketDto MapToDto(dynamic row) => new(
        (int)row.Id,
        (decimal)row.SalaryBracket,
        (decimal)row.LaborInsuranceEmployee,
        (decimal)row.HealthInsuranceEmployee,
        (DateTime)row.CreatedAt);
}
