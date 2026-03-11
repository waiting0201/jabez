namespace Jabez.Api.Models.Entities;

/// <summary>Junction table: Role ↔ Permission (many-to-many)</summary>
public class RolePermission
{
    public string RoleId       { get; set; } = string.Empty;
    public string PermissionId { get; set; } = string.Empty;

    // Navigation
    public Role       Role       { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
