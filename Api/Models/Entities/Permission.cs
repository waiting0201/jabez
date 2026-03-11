namespace Jabez.Api.Models.Entities;

public class Permission
{
    public string  Id          { get; set; } = string.Empty;
    public string  Code        { get; set; } = string.Empty; // e.g. "users:read"
    public string  Name        { get; set; } = string.Empty;
    public string  Module      { get; set; } = string.Empty; // e.g. "Users"
    public string? Description { get; set; }

    // Navigation
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
