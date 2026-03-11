namespace Jabez.Api.Models.Entities;

/// <summary>Junction table: User ↔ Role (many-to-many)</summary>
public class UserRole
{
    public Guid   UserId { get; set; }
    public string RoleId { get; set; } = string.Empty;

    // Navigation
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
