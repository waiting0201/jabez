namespace Jabez.Api.Models.Dtos;

public sealed record VendorDto(
    int      Id,
    string   Name,
    string?  TaxId,
    string?  IdNumber,
    string?  Phone,
    string?  ContactPerson,
    string?  Address,
    string?  BankAccountName,
    string?  BankName,
    string?  BankCode,
    string?  BankAccount,
    string?  BankBookImageUrl,
    string?  IdCardFrontUrl,
    string?  IdCardBackUrl,
    string?  Note,
    bool     IsActive,
    int      UsageCount,
    DateTime CreatedAt);

/// <summary>輕量級廠商資料（供下拉選單用，不需 vendors:read 權限）</summary>
public sealed record VendorLookupDto(
    int     Id,
    string  Name,
    string? TaxId,
    string? IdNumber);

public sealed record CreateVendorRequest(
    string  Name,
    string? TaxId         = null,
    string? IdNumber      = null,
    string? Phone         = null,
    string? ContactPerson = null,
    string? Address         = null,
    string? BankAccountName = null,
    string? BankName        = null,
    string? BankCode        = null,
    string? BankAccount     = null,
    string? Note            = null,
    bool    IsActive        = true);

public sealed record UpdateVendorRequest(
    string? Name,
    string? TaxId,
    string? IdNumber,
    string? Phone,
    string? ContactPerson,
    string? Address,
    string? BankAccountName,
    string? BankName,
    string? BankCode,
    string? BankAccount,
    string? Note,
    bool?   IsActive);

/// <summary>GCIS 統編查詢回應（廠商名稱、地址、負責人）</summary>
public sealed record VendorTaxIdLookupResponse(
    string  TaxId,
    string  Name,
    string? Address,
    string? ContactPerson);
