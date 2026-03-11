using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

public class RefreshToken
{
    public int      Id        { get; set; }
    public string   Token     { get; set; } = string.Empty;
    public Guid     UserId    { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool     IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; } = Clock.Now;

    // Navigation
    public User User { get; set; } = null!;
}
