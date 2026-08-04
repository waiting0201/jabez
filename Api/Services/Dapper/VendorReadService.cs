using Dapper;
using Jabez.Api.Common;
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

    /// <summary>關鍵字篩選片段（模糊比對名稱 / 統編 / 身分證字號 / 聯絡人 / 電話），供 GetAllAsync 與 GetPagedAsync 共用</summary>
    private static (string WhereClause, string? SearchParam) BuildSearchFilter(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return (string.Empty, null);

        const string clause = """
            WHERE v.Name LIKE @Search OR v.TaxId LIKE @Search OR v.IdNumber LIKE @Search
               OR v.ContactPerson LIKE @Search OR v.Phone LIKE @Search
            """;
        return (clause, $"%{search.Trim()}%");
    }

    /// <summary>廠商清單（不分頁，供下拉 / 匯出等情境）；search 有值時以關鍵字模糊比對</summary>
    public async Task<IEnumerable<VendorDto>> GetAllAsync(string? search = null)
    {
        var (whereClause, searchParam) = BuildSearchFilter(search);

        var sql = $"""
            {BaseSelect}
            {whereClause}
            {GroupByCols}
            ORDER BY v.Name
            """;

        var rows = await db.QueryAsync<dynamic>(sql, new { Search = searchParam });

        return rows.Select(MapVendor);
    }

    /// <summary>廠商清單（分頁）；search 有值時以關鍵字模糊比對名稱 / 統編 / 身分證字號 / 聯絡人 / 電話</summary>
    public async Task<PagedResult<VendorDto>> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var (whereClause, searchParam) = BuildSearchFilter(search);

        var countSql = $"SELECT COUNT(*) FROM Vendors v {whereClause}";
        var sql = $"""
            {BaseSelect}
            {whereClause}
            {GroupByCols}
            ORDER BY v.Name
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            """;

        var param = new { Search = searchParam, Skip = (page - 1) * pageSize, Take = pageSize };

        int total = await db.ExecuteScalarAsync<int>(countSql, param);
        var rows = await db.QueryAsync<dynamic>(sql, param);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);

        return new PagedResult<VendorDto>(rows.Select(MapVendor), total, page, pageSize, Math.Max(1, totalPages));
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
