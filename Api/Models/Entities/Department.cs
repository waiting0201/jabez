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
    public bool     CanViewSiblings { get; set; } = false;   // 專案可見性：同 ParentId 兄弟部門專案是否可見
    public DateTime CreatedAt   { get; set; } = Clock.Now;

    // Navigation
    public Department?              Parent   { get; set; }
    public ICollection<Department>  Children { get; set; } = [];
    public ICollection<User>        Users    { get; set; } = [];
}
