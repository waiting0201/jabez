using System.Security.Claims;

namespace Jabez.Api.Services;

/// <summary>
/// 使用者可見專案的範圍。規則見 CLAUDE.md「專案可見性規則」章節。
/// </summary>
/// <param name="SeeAll">true → 不過濾；false → 僅 <see cref="AllowedDepartmentIds"/> 中的部門可見</param>
/// <param name="AllowedDepartmentIds">當 SeeAll=false 時，可見的部門 Id 清單（空清單 → 不可見任何專案）</param>
public sealed record ProjectAccessScope(
    bool SeeAll,
    IReadOnlyList<int> AllowedDepartmentIds);

public interface IProjectAccessResolver
{
    /// <summary>依使用者 JWT claims 解析可見專案範圍。</summary>
    Task<ProjectAccessScope> ResolveAsync(ClaimsPrincipal user);
}
