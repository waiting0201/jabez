using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

public class Role
{
    public string   Id          { get; set; } = string.Empty; // e.g. "admin", "manager"
    public string   Name        { get; set; } = string.Empty;
    public string?  Description { get; set; }
    public DateTime CreatedAt   { get; set; } = Clock.Now;

    // Navigation
    public ICollection<UserRole>       UserRoles       { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
