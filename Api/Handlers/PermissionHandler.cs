using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Handlers;

public sealed class PermissionHandler(AppDbContext db, IPermissionReadService reader)
{
    private static PermissionDto ToDto(Permission p) =>
        new(p.Id, p.Code, p.Name, p.Module, p.Description);

    public async Task<IActionResult> GetAllAsync()
    {
        var perms = await reader.GetAllAsync();
        return new OkObjectResult(ApiResponse.Ok(perms));
    }

    public async Task<IActionResult> GetByIdAsync(string id)
    {
        var perm = await reader.GetByIdAsync(id);
        return perm is null
            ? new NotFoundObjectResult(ApiResponse.Fail("Permission not found."))
            : new OkObjectResult(ApiResponse.Ok(perm));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<CreatePermissionRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (await db.Permissions.AnyAsync(p => p.Code == body.Code))
            throw AppException.Conflict($"Permission code '{body.Code}' already exists.");

        // 自動產生遞增整數 Id
        var maxId = await db.Permissions
            .Select(p => p.Id)
            .ToListAsync();
        var nextId = (maxId.Select(id => int.TryParse(id, out var n) ? n : 0).DefaultIfEmpty(0).Max() + 1).ToString();

        var perm = new Permission
        {
            Id          = nextId,
            Code        = body.Code,
            Name        = body.Name,
            Module      = body.Module,
            Description = body.Description,
        };
        db.Permissions.Add(perm);
        await db.SaveChangesAsync();

        return new ObjectResult(ApiResponse.Ok(ToDto(perm), "Permission created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var body = await req.ReadFromJsonAsync<UpdatePermissionRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var perm = await db.Permissions.FindAsync(id)
            ?? throw AppException.NotFound("Permission");

        if (body.Code is not null && body.Code != perm.Code)
        {
            if (await db.Permissions.AnyAsync(p => p.Code == body.Code))
                throw AppException.Conflict($"Permission code '{body.Code}' already exists.");
            perm.Code = body.Code;
        }
        if (body.Name        is not null) perm.Name        = body.Name;
        if (body.Module      is not null) perm.Module      = body.Module;
        if (body.Description is not null) perm.Description = body.Description;

        await db.SaveChangesAsync();
        return new OkObjectResult(ApiResponse.Ok(ToDto(perm), "Permission updated."));
    }

    public async Task<IActionResult> DeleteAsync(string id)
    {
        var perm = await db.Permissions.FindAsync(id)
            ?? throw AppException.NotFound("Permission");

        var usedByRoles = await db.RolePermissions.AnyAsync(rp => rp.PermissionId == id);
        if (usedByRoles)
            throw AppException.Conflict("此權限已被角色使用，無法刪除。請先移除相關角色的權限設定。");

        db.Permissions.Remove(perm);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Permission '{id}' deleted."));
    }
}
