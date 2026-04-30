using System.Security.Claims;

namespace Jabez.Api.Services;

/// <summary>
/// 使用者可見資料的部門範圍。規則見 CLAUDE.md「部門可見性規則」章節。
/// 雖名稱含「Project」字眼，實際語意已是通用部門 scope，套用於 Project.DepartmentId 與 User.DepartmentId 兩類過濾欄位。
/// </summary>
/// <param name="SeeAll">true → 不過濾；false → 僅 <see cref="AllowedDepartmentIds"/> 中的部門可見</param>
/// <param name="AllowedDepartmentIds">當 SeeAll=false 時，可見的部門 Id 清單（空清單 → 不可見任何資料）</param>
public sealed record ProjectAccessScope(
    bool SeeAll,
    IReadOnlyList<int> AllowedDepartmentIds);

public interface IProjectAccessResolver
{
    /// <summary>依使用者 JWT claims 與 Department 旗標（CanSeeAll / CanViewSiblings / CanViewDescendants）解析可見部門範圍。</summary>
    Task<ProjectAccessScope> ResolveAsync(ClaimsPrincipal user);
}
