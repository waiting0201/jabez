using Dapper;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class VendorReadService(IDbConnection db) : IVendorReadService
{
    private const string BaseSelect = """
        SELECT v.Id, v.Name, v.TaxId, v.Phone, v.ContactPerson, v.Address,
               v.BankAccount, v.Note, v.IsActive, v.CreatedAt,
               COUNT(pr.Id) AS UsageCount
        FROM Vendors v
        LEFT JOIN PaymentRequests pr ON pr.VendorId = v.Id
        """;

    public async Task<IEnumerable<VendorDto>> GetAllAsync()
    {
        const string sql = $"""
            {BaseSelect}
            GROUP BY v.Id, v.Name, v.TaxId, v.Phone, v.ContactPerson, v.Address,
                     v.BankAccount, v.Note, v.IsActive, v.CreatedAt
            ORDER BY v.Name
            """;

        var rows = await db.QueryAsync<dynamic>(sql);

        return rows.Select(row => new VendorDto(
            (int)row.Id,
            (string)row.Name,
            (string?)row.TaxId,
            (string?)row.Phone,
            (string?)row.ContactPerson,
            (string?)row.Address,
            (string?)row.BankAccount,
            (string?)row.Note,
            (bool)row.IsActive,
            (int)row.UsageCount,
            (DateTime)row.CreatedAt));
    }

    /// <summary>輕量級廠商清單（供下拉選單，不需 vendors:read 權限；僅回 IsActive=1）</summary>
    public async Task<IEnumerable<VendorLookupDto>> GetLookupAsync()
    {
        const string sql = "SELECT Id, Name, TaxId FROM Vendors WHERE IsActive = 1 ORDER BY Name";
        return await db.QueryAsync<VendorLookupDto>(sql);
    }

    public async Task<VendorDto?> GetByIdAsync(int id)
    {
        const string sql = $"""
            {BaseSelect}
            WHERE v.Id = @Id
            GROUP BY v.Id, v.Name, v.TaxId, v.Phone, v.ContactPerson, v.Address,
                     v.BankAccount, v.Note, v.IsActive, v.CreatedAt
            """;

        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (row is null) return null;

        return new VendorDto(
            (int)row.Id,
            (string)row.Name,
            (string?)row.TaxId,
            (string?)row.Phone,
            (string?)row.ContactPerson,
            (string?)row.Address,
            (string?)row.BankAccount,
            (string?)row.Note,
            (bool)row.IsActive,
            (int)row.UsageCount,
            (DateTime)row.CreatedAt);
    }
}
