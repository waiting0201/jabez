using System.Security.Claims;
using Jabez.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 部門可見性解析器（規則見 CLAUDE.md「部門可見性規則」章節）：
/// 1. Superadmin → SeeAll
/// 2. Department.CanSeeAll = true → SeeAll（取代寫死的財務體系部門 Code 判定）
/// 3. 其他員工 → 自己部門
///    + 若 CanViewSiblings=true 加同 ParentId 兄弟部門
///    + 若 CanViewDescendants=true 加所有遞迴下層子部門
/// 兩個 (sibling / descendants) 旗標可同時生效，採聯集。
/// </summary>
public sealed class ProjectAccessResolver(AppDbContext db) : IProjectAccessResolver
{
    public async Task<ProjectAccessScope> ResolveAsync(ClaimsPrincipal user)
    {
        // Rule 1: Superadmin
        if (string.Equals(user.FindFirstValue("is_superadmin"), "true", StringComparison.OrdinalIgnoreCase))
            return new ProjectAccessScope(true, []);

        if (!int.TryParse(user.FindFirstValue("department_id"), out var deptId) || deptId <= 0)
            return new ProjectAccessScope(false, []);   // 使用者無部門 → 看不到任何資料

        // 一次撈該員工部門的所有可見性旗標
        var self = await db.Departments
            .AsNoTracking()
            .Where(d => d.Id == deptId)
            .Select(d => new { d.Id, d.ParentId, d.CanSeeAll, d.CanViewSiblings, d.CanViewDescendants })
            .FirstOrDefaultAsync();

        if (self is null)
            return new ProjectAccessScope(false, []);

        // Rule 2: 部門設定 CanSeeAll=true → SeeAll
        if (self.CanSeeAll)
            return new ProjectAccessScope(true, []);

        // Rule 3: 自己部門 + 條件式擴展（同層兄弟 / 遞迴下層）
        var allowed = new HashSet<int> { self.Id };

        if (self.CanViewSiblings && self.ParentId is int parentId)
        {
            var siblings = await db.Departments
                .AsNoTracking()
                .Where(d => d.ParentId == parentId && d.Id != self.Id)
                .Select(d => d.Id)
                .ToListAsync();
            foreach (var s in siblings) allowed.Add(s);
        }

        if (self.CanViewDescendants)
        {
            foreach (var d in await GetDescendantIdsAsync(self.Id))
                allowed.Add(d);
        }

        return new ProjectAccessScope(false, [.. allowed]);
    }

    /// <summary>
    /// 取得指定部門的所有遞迴後代部門 Id（不含自己）。
    /// 一次載入全部 Departments 後在記憶體 DFS 走子節點，部門表筆數小（個位數～數十筆），成本可接受。
    /// </summary>
    private async Task<List<int>> GetDescendantIdsAsync(int rootId)
    {
        var all = await db.Departments
            .AsNoTracking()
            .Select(d => new { d.Id, d.ParentId })
            .ToListAsync();

        var byParent = all
            .Where(d => d.ParentId.HasValue)
            .GroupBy(d => d.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        var result  = new List<int>();
        var visited = new HashSet<int>();
        var stack   = new Stack<int>();
        stack.Push(rootId);

        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            if (!visited.Add(cur)) continue;  // 防呆：循環依賴
            if (!byParent.TryGetValue(cur, out var children)) continue;

            foreach (var c in children)
            {
                result.Add(c);
                stack.Push(c);
            }
        }
        return result;
    }
}
