using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Handlers;

public sealed class DepartmentHandler(AppDbContext db, IDepartmentReadService reader)
{
    public async Task<IActionResult> GetAllAsync()
    {
        var depts = await reader.GetAllAsync();
        return new OkObjectResult(ApiResponse.Ok(depts));
    }

    public async Task<IActionResult> GetByIdAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid department ID format."));

        var dept = await reader.GetByIdAsync(intId);
        return dept is null
            ? new NotFoundObjectResult(ApiResponse.Fail("Department not found.", $"No department with id '{id}'."))
            : new OkObjectResult(ApiResponse.Ok(dept));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<CreateDepartmentRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (string.IsNullOrWhiteSpace(body.Name))
            return new BadRequestObjectResult(ApiResponse.Fail("Name is required."));

        if (body.Code is not null && await db.Departments.AnyAsync(d => d.Code == body.Code))
            throw AppException.Conflict($"Department code '{body.Code}' is already in use.");

        var dept = new Department
        {
            Name               = body.Name,
            Code               = body.Code,
            Description        = body.Description,
            ParentId           = body.ParentId,
            SortOrder          = body.SortOrder,
            CanViewSiblings    = body.CanViewSiblings,
            CanSeeAll          = body.CanSeeAll,
            CanViewDescendants = body.CanViewDescendants,
            CanViewParent      = body.CanViewParent,
            CreatedAt          = Clock.Now,
        };
        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(dept.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Department created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid department ID format."));

        var body = await req.ReadFromJsonAsync<UpdateDepartmentRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var dept = await db.Departments.FindAsync(intId)
            ?? throw AppException.NotFound("Department");

        if (body.Name        is not null) dept.Name        = body.Name;
        if (body.Code        is not null) dept.Code        = body.Code;
        if (body.Description is not null) dept.Description = body.Description;
        if (body.ParentId.HasValue)             dept.ParentId           = body.ParentId == 0 ? null : body.ParentId;
        if (body.SortOrder.HasValue)            dept.SortOrder          = body.SortOrder.Value;
        if (body.CanViewSiblings.HasValue)      dept.CanViewSiblings    = body.CanViewSiblings.Value;
        if (body.CanSeeAll.HasValue)            dept.CanSeeAll          = body.CanSeeAll.Value;
        if (body.CanViewDescendants.HasValue)   dept.CanViewDescendants = body.CanViewDescendants.Value;
        if (body.CanViewParent.HasValue)        dept.CanViewParent      = body.CanViewParent.Value;

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(dept.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Department updated."));
    }

    public async Task<IActionResult> DeleteAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid department ID format."));

        var dept = await db.Departments.FindAsync(intId)
            ?? throw AppException.NotFound("Department");

        // 防止刪除有子部門的部門
        if (await db.Departments.AnyAsync(d => d.ParentId == intId))
            throw AppException.BadRequest("Cannot delete department with sub-departments.");

        db.Departments.Remove(dept);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Department '{id}' deleted."));
    }
}
