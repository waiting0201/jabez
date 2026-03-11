using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

public class JobTitle
{
    public int      Id          { get; set; }
    public string   Name        { get; set; } = string.Empty;
    public int      Level       { get; set; }
    public string?  Description { get; set; }
    public DateTime CreatedAt   { get; set; } = Clock.Now;

    // Navigation
    public ICollection<User> Users { get; set; } = [];
}
