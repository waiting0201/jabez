using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Jabez.Api.Handlers;

public sealed class RoleHandler(AppDbContext db, IRoleReadService reader)
{
    public async Task<IActionResult> GetAllAsync()
    {
        var roles = await reader.GetAllAsync();
        return new OkObjectResult(ApiResponse.Ok(roles));
    }

    public async Task<IActionResult> GetByIdAsync(string id)
    {
        var role = await reader.GetByIdAsync(id);
        return role is null
            ? new NotFoundObjectResult(ApiResponse.Fail("Role not found.", $"No role with id '{id}'."))
            : new OkObjectResult(ApiResponse.Ok(role));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<CreateRoleRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var roleId = string.IsNullOrWhiteSpace(body.Id) ? Guid.NewGuid().ToString() : body.Id;

        if (await db.Roles.AnyAsync(r => r.Id == roleId))
            throw AppException.Conflict($"Role id '{roleId}' already exists.");

        EnsurePermissionsWithinOperator(req.HttpContext.User, body.PermissionCodes ?? [], []);

        var role = new Role
        {
            Id          = roleId,
            Name        = body.Name,
            Description = body.Description,
            CreatedAt   = Clock.Now,
        };
        db.Roles.Add(role);

        foreach (var permId in body.PermissionCodes ?? [])
        {
            // PermissionCodes 傳入的是 code，找對應 permission id
            var perm = await db.Permissions.FirstOrDefaultAsync(p => p.Code == permId);
            if (perm is not null)
                db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
        }

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(role.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Role created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var body = await req.ReadFromJsonAsync<UpdateRoleRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var role = await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw AppException.NotFound("Role");

        await EnsureNotOwnRoleAsync(req.HttpContext.User, role.Id);
        if (body.PermissionCodes is not null)
        {
            var currentCodes = await db.RolePermissions.Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.Permission.Code).ToListAsync();
            EnsurePermissionsWithinOperator(req.HttpContext.User, body.PermissionCodes, currentCodes);
        }

        if (body.Name        is not null) role.Name        = body.Name;
        if (body.Description is not null) role.Description = body.Description;

        if (body.PermissionCodes is not null)
        {
            db.RolePermissions.RemoveRange(role.RolePermissions);
            foreach (var code in body.PermissionCodes)
            {
                var perm = await db.Permissions.FirstOrDefaultAsync(p => p.Code == code);
                if (perm is not null)
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
            }
        }

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(role.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Role updated."));
    }

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var role = await db.Roles.FindAsync(id)
            ?? throw AppException.NotFound("Role");

        await EnsureNotOwnRoleAsync(req.HttpContext.User, role.Id);

        db.Roles.Remove(role);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Role '{id}' deleted."));
    }

    /// <summary>
    /// 防提權（2026-10 安全修補）：非 Superadmin 不可新增 / 修改 / 刪除**自己所屬**的角色 ——
    /// 否則持 roles:write 者可替自己的角色加上任何權限。與 UserHandler.EnsureCanAssignRolesAsync 同一套規則。
    /// </summary>
    private async Task EnsureNotOwnRoleAsync(ClaimsPrincipal op, string roleId)
    {
        if (op.FindFirst("is_superadmin")?.Value == "true") return;
        if (!Guid.TryParse(op.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var operatorId)) return;
        if (await db.UserRoles.AnyAsync(ur => ur.UserId == operatorId && ur.RoleId == roleId))
            throw AppException.Forbidden("不可修改自己所屬的角色。");
    }

    /// <summary>非 Superadmin 新加入角色的權限碼必須是自身權限（JWT permissions claim）的子集合；移除權限屬降權不檢查</summary>
    private static void EnsurePermissionsWithinOperator(
        ClaimsPrincipal op, IEnumerable<string> requested, IEnumerable<string> current)
    {
        if (op.FindFirst("is_superadmin")?.Value == "true") return;
        var own     = op.FindAll("permissions").Select(c => c.Value).ToHashSet();
        var existed = current.ToHashSet();
        var missing = requested.Where(c => !string.IsNullOrEmpty(c) && !existed.Contains(c) && !own.Contains(c))
                               .Distinct().ToArray();
        if (missing.Length > 0)
            throw AppException.Forbidden($"不可授予超出自身權限的權限（缺少：{string.Join("、", missing)}）。");
    }
}
