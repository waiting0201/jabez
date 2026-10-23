using Dapper;
using Jabez.Api.Services.Dapper;
using System.Data;

namespace Jabez.Api.Services;

/// <summary>
/// 員工「工作地縣市」解析（天然災害停班用）：
/// <c>User.WorkCounty ?? Department.County ?? 沿 ParentId 往上最近一個有設定的部門 County ?? null</c>。
/// null ＝ 未設定工作地，颱風假不套用，並列入 HR 通知的「未設定工作地」人數。
///
/// 解析規則收斂在 <see cref="EffectiveCountyCte"/> 這一段 SQL（讀取服務與本類別共用），
/// 不在 C# 端逐人走訪部門樹。request-scoped memo，比照 <see cref="WorkPatternReadService"/>。
/// </summary>
public interface IWorkCountyResolver
{
    Task<string?> ResolveAsync(Guid userId);

    /// <summary>某縣市目前有哪些在職員工（工作地解析結果等於該縣市者）。</summary>
    Task<IReadOnlyList<Guid>> UserIdsInCountyAsync(string county);

    /// <summary>在職員工中工作地解析為 null（未設定）的人數。</summary>
    Task<int> CountWithoutCountyAsync();
}

public sealed class WorkCountyResolver(IDbConnection db) : IWorkCountyResolver
{
    /// <summary>
    /// 部門有效縣市（含祖先繼承）的 CTE：<c>DeptCounty(RootId, County)</c>，RootId ＝ 員工所屬部門。
    /// 遞迴只在「目前節點尚無縣市」時才往上一層，因此每個 RootId 至多一列（最近祖先優先）。
    /// 使用端須接 <c>OPTION (MAXRECURSION 50)</c>。
    /// </summary>
    public const string EffectiveCountyCte = """
        WITH DeptChain AS (
            SELECT Id AS RootId, ParentId, County, 0 AS Lvl FROM Departments
            UNION ALL
            SELECT c.RootId, p.ParentId, p.County, c.Lvl + 1
            FROM DeptChain c
            JOIN Departments p ON p.Id = c.ParentId
            WHERE c.County IS NULL AND c.Lvl < 20
        ),
        DeptCounty AS (SELECT RootId, County FROM DeptChain WHERE County IS NOT NULL)
        """;

    private readonly Dictionary<Guid, string?> _memo = [];

    public async Task<string?> ResolveAsync(Guid userId)
    {
        if (_memo.TryGetValue(userId, out var cached)) return cached;

        var sql = $"""
            {EffectiveCountyCte}
            SELECT COALESCE(u.WorkCounty, dc.County)
            FROM Users u
            LEFT JOIN DeptCounty dc ON dc.RootId = u.DepartmentId
            WHERE u.Id = @UserId
            OPTION (MAXRECURSION 50)
            """;
        var value = await db.ExecuteScalarAsync<string?>(sql, new { UserId = userId });
        return _memo[userId] = value;
    }

    public async Task<IReadOnlyList<Guid>> UserIdsInCountyAsync(string county)
    {
        var sql = $"""
            {EffectiveCountyCte}
            SELECT u.Id
            FROM Users u
            LEFT JOIN DeptCounty dc ON dc.RootId = u.DepartmentId
            WHERE u.IsSuperAdmin = 0 AND u.Status = 'active'
              AND COALESCE(u.WorkCounty, dc.County) = @County
            OPTION (MAXRECURSION 50)
            """;
        return (await db.QueryAsync<Guid>(sql, new { County = county })).ToList();
    }

    public async Task<int> CountWithoutCountyAsync()
    {
        var sql = $"""
            {EffectiveCountyCte}
            SELECT COUNT(*)
            FROM Users u
            LEFT JOIN DeptCounty dc ON dc.RootId = u.DepartmentId
            WHERE u.IsSuperAdmin = 0 AND u.Status = 'active'
              AND COALESCE(u.WorkCounty, dc.County) IS NULL
            OPTION (MAXRECURSION 50)
            """;
        return await db.ExecuteScalarAsync<int>(sql);
    }
}
