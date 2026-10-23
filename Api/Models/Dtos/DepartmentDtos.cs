namespace Jabez.Api.Models.Dtos;

public sealed record DepartmentDto(
    int      Id,
    string   Name,
    string?  Code,
    string?  Description,
    int?     ParentId,
    string?  ParentName,
    int      SortOrder,
    bool     CanViewSiblings,
    bool     CanSeeAll,
    bool     CanViewDescendants,
    bool     CanViewParent,
    int      EmployeeCount,
    DateTime CreatedAt,
    string?  EmployeeNames,
    /// <summary>所在縣市代碼（TaiwanCounties）；null ＝ 未設定（颱風假沿用上層部門）</summary>
    string?  County = null);

public sealed record CreateDepartmentRequest(
    string  Name,
    string? Code               = null,
    string? Description        = null,
    int?    ParentId           = null,
    int     SortOrder          = 0,
    bool    CanViewSiblings    = false,
    bool    CanSeeAll          = false,
    bool    CanViewDescendants = false,
    bool    CanViewParent      = false,
    string? County             = null);

public sealed record UpdateDepartmentRequest(
    string?  Name,
    string?  Code,
    string?  Description,
    int?     ParentId,
    int?     SortOrder,
    bool?    CanViewSiblings,
    bool?    CanSeeAll,
    bool?    CanViewDescendants,
    bool?    CanViewParent,
    /// <summary>null ＝ 不變更；空字串 / 空白 ＝ 清除；其餘須為 TaiwanCounties 代碼</summary>
    string?  County = null);

/// <summary>輕量部門項目（供下拉選單；不需 departments:read）。</summary>
public sealed record DepartmentLookupDto(int Id, string Name, int? ParentId);
