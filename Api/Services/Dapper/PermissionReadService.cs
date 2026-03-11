using Dapper;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class PermissionReadService(IDbConnection db) : IPermissionReadService
{
    public async Task<IEnumerable<PermissionDto>> GetAllAsync()
    {
        const string sql = """
            SELECT Id, Code, Name, Module, Description
            FROM Permissions
            ORDER BY Module, Code
            """;

        var rows = await db.QueryAsync<dynamic>(sql);

        return rows.Select(r => new PermissionDto(
            (string)r.Id,
            (string)r.Code,
            (string)r.Name,
            (string)r.Module,
            (string?)r.Description));
    }

    public async Task<PermissionDto?> GetByIdAsync(string id)
    {
        const string sql = """
            SELECT Id, Code, Name, Module, Description
            FROM Permissions
            WHERE Id = @Id
            """;

        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (row is null) return null;

        return new PermissionDto(
            (string)row.Id,
            (string)row.Code,
            (string)row.Name,
            (string)row.Module,
            (string?)row.Description);
    }
}
