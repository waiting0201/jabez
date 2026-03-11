using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        if (await db.Roles.AnyAsync(r => r.Id == body.Id))
            throw AppException.Conflict($"Role id '{body.Id}' already exists.");

        var role = new Role
        {
            Id          = body.Id,
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

    public async Task<IActionResult> DeleteAsync(string id)
    {
        var role = await db.Roles.FindAsync(id)
            ?? throw AppException.NotFound("Role");

        db.Roles.Remove(role);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Role '{id}' deleted."));
    }
}
