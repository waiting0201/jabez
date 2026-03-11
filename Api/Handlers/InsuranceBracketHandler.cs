using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

public sealed class InsuranceBracketHandler(AppDbContext db, IInsuranceBracketReadService reader)
{
    /// <summary>取得勞健保級距列表</summary>
    public async Task<IActionResult> GetAllAsync()
    {
        var brackets = await reader.GetAllAsync();
        return new OkObjectResult(ApiResponse.Ok(brackets));
    }

    /// <summary>根據薪資查詢對應的勞健保級距</summary>
    public async Task<IActionResult> LookupBySalaryAsync(HttpRequest req)
    {
        if (!decimal.TryParse(req.Query["salary"], out var salary) || salary <= 0)
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的薪資參數。"));

        var bracket = await reader.GetBySalaryAsync(salary);
        return bracket is null
            ? new NotFoundObjectResult(ApiResponse.Fail("找不到對應的勞健保級距。"))
            : new OkObjectResult(ApiResponse.Ok(bracket));
    }

    /// <summary>取得單筆勞健保級距</summary>
    public async Task<IActionResult> GetByIdAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid insurance bracket ID format."));

        var bracket = await reader.GetByIdAsync(intId);
        return bracket is null
            ? new NotFoundObjectResult(ApiResponse.Fail("Insurance bracket not found.", $"No bracket with id '{id}'."))
            : new OkObjectResult(ApiResponse.Ok(bracket));
    }

    /// <summary>新增勞健保級距</summary>
    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<CreateInsuranceBracketRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (body.SalaryBracket <= 0)
            return new BadRequestObjectResult(ApiResponse.Fail("SalaryBracket must be positive."));

        var entity = new InsuranceBracket
        {
            SalaryBracket           = body.SalaryBracket,
            LaborInsuranceEmployee  = body.LaborInsuranceEmployee,
            HealthInsuranceEmployee = body.HealthInsuranceEmployee,
            CreatedAt               = Clock.Now,
        };
        db.InsuranceBrackets.Add(entity);
        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(entity.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Insurance bracket created.")) { StatusCode = 201 };
    }

    /// <summary>更新勞健保級距</summary>
    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid insurance bracket ID format."));

        var body = await req.ReadFromJsonAsync<UpdateInsuranceBracketRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var entity = await db.InsuranceBrackets.FindAsync(intId)
            ?? throw AppException.NotFound("InsuranceBracket");

        if (body.SalaryBracket.HasValue)           entity.SalaryBracket           = body.SalaryBracket.Value;
        if (body.LaborInsuranceEmployee.HasValue)  entity.LaborInsuranceEmployee  = body.LaborInsuranceEmployee.Value;
        if (body.HealthInsuranceEmployee.HasValue) entity.HealthInsuranceEmployee = body.HealthInsuranceEmployee.Value;

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(entity.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Insurance bracket updated."));
    }

    /// <summary>刪除勞健保級距</summary>
    public async Task<IActionResult> DeleteAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid insurance bracket ID format."));

        var entity = await db.InsuranceBrackets.FindAsync(intId)
            ?? throw AppException.NotFound("InsuranceBracket");

        db.InsuranceBrackets.Remove(entity);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok<string?>(null, "Insurance bracket deleted."));
    }
}
