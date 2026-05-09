using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Handlers;

public sealed class VendorHandler(AppDbContext db, IVendorReadService reader)
{
    // GET /api/vendors/lookup — 輕量級廠商清單（供下拉選單，不需 vendors:read 權限）
    public async Task<IActionResult> GetLookupAsync()
    {
        var list = await reader.GetLookupAsync();
        return new OkObjectResult(ApiResponse.Ok(list));
    }

    public async Task<IActionResult> GetAllAsync()
    {
        var vendors = await reader.GetAllAsync();
        return new OkObjectResult(ApiResponse.Ok(vendors));
    }

    public async Task<IActionResult> GetByIdAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid vendor ID format."));

        var vendor = await reader.GetByIdAsync(intId);
        return vendor is null
            ? new NotFoundObjectResult(ApiResponse.Fail("Vendor not found.", $"No vendor with id '{id}'."))
            : new OkObjectResult(ApiResponse.Ok(vendor));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<CreateVendorRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var name = body.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return new BadRequestObjectResult(ApiResponse.Fail("廠商名稱為必填。"));

        var taxId = string.IsNullOrWhiteSpace(body.TaxId) ? null : body.TaxId.Trim();

        if (taxId is not null && await db.Vendors.AnyAsync(v => v.TaxId == taxId))
            return new BadRequestObjectResult(ApiResponse.Fail($"統編「{taxId}」已存在。"));

        var vendor = new Vendor
        {
            Name          = name,
            TaxId         = taxId,
            Phone         = string.IsNullOrWhiteSpace(body.Phone)         ? null : body.Phone.Trim(),
            ContactPerson = string.IsNullOrWhiteSpace(body.ContactPerson) ? null : body.ContactPerson.Trim(),
            Address       = string.IsNullOrWhiteSpace(body.Address)       ? null : body.Address.Trim(),
            BankAccount   = string.IsNullOrWhiteSpace(body.BankAccount)   ? null : body.BankAccount.Trim(),
            Note          = string.IsNullOrWhiteSpace(body.Note)          ? null : body.Note.Trim(),
            IsActive      = body.IsActive,
            CreatedAt     = Clock.Now,
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(vendor.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Vendor created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid vendor ID format."));

        var body = await req.ReadFromJsonAsync<UpdateVendorRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var vendor = await db.Vendors.FindAsync(intId)
            ?? throw AppException.NotFound("Vendor");

        if (body.Name is not null)
        {
            var name = body.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return new BadRequestObjectResult(ApiResponse.Fail("廠商名稱不可為空。"));
            vendor.Name = name;
        }

        if (body.TaxId is not null)
        {
            var taxId = string.IsNullOrWhiteSpace(body.TaxId) ? null : body.TaxId.Trim();
            if (taxId != vendor.TaxId
                && taxId is not null
                && await db.Vendors.AnyAsync(v => v.Id != intId && v.TaxId == taxId))
                return new BadRequestObjectResult(ApiResponse.Fail($"統編「{taxId}」已存在。"));
            vendor.TaxId = taxId;
        }

        if (body.Phone         is not null) vendor.Phone         = string.IsNullOrWhiteSpace(body.Phone)         ? null : body.Phone.Trim();
        if (body.ContactPerson is not null) vendor.ContactPerson = string.IsNullOrWhiteSpace(body.ContactPerson) ? null : body.ContactPerson.Trim();
        if (body.Address       is not null) vendor.Address       = string.IsNullOrWhiteSpace(body.Address)       ? null : body.Address.Trim();
        if (body.BankAccount   is not null) vendor.BankAccount   = string.IsNullOrWhiteSpace(body.BankAccount)   ? null : body.BankAccount.Trim();
        if (body.Note          is not null) vendor.Note          = string.IsNullOrWhiteSpace(body.Note)          ? null : body.Note.Trim();
        if (body.IsActive.HasValue)         vendor.IsActive      = body.IsActive.Value;

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(vendor.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Vendor updated."));
    }

    public async Task<IActionResult> DeleteAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid vendor ID format."));

        var vendor = await db.Vendors.FindAsync(intId)
            ?? throw AppException.NotFound("Vendor");

        if (await db.PaymentRequests.AnyAsync(p => p.VendorId == intId))
            return new BadRequestObjectResult(ApiResponse.Fail(
                "此廠商已被請款單引用，無法刪除。請改用「停用」（將 IsActive 設為 false）。"));

        db.Vendors.Remove(vendor);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Vendor '{id}' deleted."));
    }
}
