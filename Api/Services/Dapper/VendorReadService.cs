using Dapper;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class VendorReadService(IDbConnection db) : IVendorReadService
{
    private const string BaseSelect = """
        SELECT v.Id, v.Name, v.TaxId, v.IdNumber, v.Phone, v.ContactPerson, v.Address,
               v.BankAccount, v.BankBookImageUrl, v.IdCardFrontUrl, v.IdCardBackUrl,
               v.Note, v.IsActive, v.CreatedAt,
               COUNT(pr.Id) AS UsageCount
        FROM Vendors v
        LEFT JOIN PaymentRequests pr ON pr.VendorId = v.Id
        """;

    private const string GroupByCols = """
        GROUP BY v.Id, v.Name, v.TaxId, v.IdNumber, v.Phone, v.ContactPerson, v.Address,
                 v.BankAccount, v.BankBookImageUrl, v.IdCardFrontUrl, v.IdCardBackUrl,
                 v.Note, v.IsActive, v.CreatedAt
        """;

    public async Task<IEnumerable<VendorDto>> GetAllAsync()
    {
        const string sql = $"""
            {BaseSelect}
            {GroupByCols}
            ORDER BY v.Name
            """;

        var rows = await db.QueryAsync<dynamic>(sql);

        return rows.Select(MapVendor);
    }

    /// <summary>輕量級廠商清單（供下拉選單，不需 vendors:read 權限；僅回 IsActive=1）</summary>
    public async Task<IEnumerable<VendorLookupDto>> GetLookupAsync()
    {
        const string sql = "SELECT Id, Name, TaxId, IdNumber FROM Vendors WHERE IsActive = 1 ORDER BY Name";
        return await db.QueryAsync<VendorLookupDto>(sql);
    }

    public async Task<VendorDto?> GetByIdAsync(int id)
    {
        const string sql = $"""
            {BaseSelect}
            WHERE v.Id = @Id
            {GroupByCols}
            """;

        var row = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapVendor(row);
    }

    private static VendorDto MapVendor(dynamic row) => new(
        (int)row.Id,
        (string)row.Name,
        (string?)row.TaxId,
        (string?)row.IdNumber,
        (string?)row.Phone,
        (string?)row.ContactPerson,
        (string?)row.Address,
        (string?)row.BankAccount,
        (string?)row.BankBookImageUrl,
        (string?)row.IdCardFrontUrl,
        (string?)row.IdCardBackUrl,
        (string?)row.Note,
        (bool)row.IsActive,
        (int)row.UsageCount,
        (DateTime)row.CreatedAt);
}
