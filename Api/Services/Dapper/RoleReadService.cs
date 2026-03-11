using Dapper;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class RoleReadService(IDbConnection db) : IRoleReadService
{
    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        const string sql = """
            SELECT
                r.Id,
                r.Name,
                r.Description,
                r.CreatedAt,
                p.Code AS PermissionCode
            FROM Roles r
            LEFT JOIN RolePermissions rp ON r.Id = rp.RoleId
            LEFT JOIN Permissions p      ON rp.PermissionId = p.Id
            ORDER BY r.CreatedAt
            """;

        var rows = await db.QueryAsync<dynamic>(sql);

        var roleDict = new Dictionary<string, (string Name, string? Description, DateTime CreatedAt, List<string> Codes)>();

        foreach (var row in rows)
        {
            var id = (string)row.Id;
            if (!roleDict.ContainsKey(id))
                roleDict[id] = (row.Name, (string?)row.Description, row.CreatedAt, []);

            if (row.PermissionCode is not null)
                roleDict[id].Codes.Add((string)row.PermissionCode);
        }

        return roleDict.Select(kv => new RoleDto(
            kv.Key,
            kv.Value.Name,
            kv.Value.Description,
            kv.Value.Codes.ToArray(),
            kv.Value.CreatedAt));
    }

    public async Task<RoleDto?> GetByIdAsync(string id)
    {
        const string sql = """
            SELECT
                r.Id,
                r.Name,
                r.Description,
                r.CreatedAt,
                p.Code AS PermissionCode
            FROM Roles r
            LEFT JOIN RolePermissions rp ON r.Id = rp.RoleId
            LEFT JOIN Permissions p      ON rp.PermissionId = p.Id
            WHERE r.Id = @Id
            """;

        var rows = await db.QueryAsync<dynamic>(sql, new { Id = id });

        RoleDto? result = null;
        var codes = new List<string>();

        foreach (var row in rows)
        {
            result ??= new RoleDto((string)row.Id, row.Name, (string?)row.Description, Array.Empty<string>(), row.CreatedAt);

            if (row.PermissionCode is not null)
                codes.Add((string)row.PermissionCode);
        }

        return result is null ? null : result with { PermissionCodes = codes.ToArray() };
    }
}
