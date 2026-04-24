using System.Security.Claims;
using Jabez.Api.Common;
using Jabez.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 專案可見性解析器（規則見 CLAUDE.md「專案可見性規則」章節）：
/// 1. Superadmin → SeeAll
/// 2. 部門 Code ∈ AC/FIN/Jabez HQ/CEO → SeeAll
/// 3. 其他員工 → 自己部門；若 Department.CanViewSiblings=true 再加上同 ParentId 兄弟部門
/// </summary>
public sealed class ProjectAccessResolver(AppDbContext db) : IProjectAccessResolver
{
    public async Task<ProjectAccessScope> ResolveAsync(ClaimsPrincipal user)
    {
        // Rule 1: Superadmin
        if (string.Equals(user.FindFirstValue("is_superadmin"), "true", StringComparison.OrdinalIgnoreCase))
            return new ProjectAccessScope(true, []);

        // Rule 2: 財務體系部門
        var deptCode = user.FindFirstValue("department_code");
        if (!string.IsNullOrEmpty(deptCode) && DepartmentCodes.FinancialAndAbove.Contains(deptCode))
            return new ProjectAccessScope(true, []);

        // Rule 3: 一般員工 — 以部門 Id 過濾；若該部門 CanViewSiblings=true 再加入同 ParentId 兄弟
        if (!int.TryParse(user.FindFirstValue("department_id"), out var deptId) || deptId <= 0)
            return new ProjectAccessScope(false, []);   // 使用者無部門 → 看不到任何專案

        var self = await db.Departments
            .AsNoTracking()
            .Where(d => d.Id == deptId)
            .Select(d => new { d.Id, d.ParentId, d.CanViewSiblings })
            .FirstOrDefaultAsync();

        if (self is null)
            return new ProjectAccessScope(false, []);

        var allowed = new List<int> { self.Id };

        if (self.CanViewSiblings && self.ParentId is int parentId)
        {
            var siblings = await db.Departments
                .AsNoTracking()
                .Where(d => d.ParentId == parentId && d.Id != self.Id)
                .Select(d => d.Id)
                .ToListAsync();
            allowed.AddRange(siblings);
        }

        return new ProjectAccessScope(false, allowed);
    }
}
