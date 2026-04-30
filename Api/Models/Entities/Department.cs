using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

public class Department
{
    public int      Id          { get; set; }
    public string   Name        { get; set; } = string.Empty;
    public string?  Code        { get; set; }
    public string?  Description { get; set; }
    public int?     ParentId    { get; set; }
    public int      SortOrder   { get; set; }
    public bool     CanViewSiblings    { get; set; } = false; // 可見性：同 ParentId 兄弟部門資料是否可見
    public bool     CanSeeAll          { get; set; } = false; // 可見性：所有部門資料皆可見（取代寫死的財務體系部門 SeeAll 判定）
    public bool     CanViewDescendants { get; set; } = false; // 可見性：所有遞迴下層子部門資料是否可見
    public bool     CanViewParent      { get; set; } = false; // 可見性：直接父部門（ParentId 指到的那一個）資料是否可見，不遞迴
    public DateTime CreatedAt   { get; set; } = Clock.Now;

    // Navigation
    public Department?              Parent   { get; set; }
    public ICollection<Department>  Children { get; set; } = [];
    public ICollection<User>        Users    { get; set; } = [];
}
