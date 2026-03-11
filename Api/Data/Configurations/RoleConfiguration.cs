using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(r => r.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(r => r.Description)
               .HasMaxLength(500);

        builder.Property(r => r.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        // Seed data
        builder.HasData(
            new Role { Id = "admin",   Name = "Administrator", Description = "Full system access",                     CreatedAt = new DateTime(2024, 1, 1,  0, 0, 0, DateTimeKind.Utc) },
            new Role { Id = "manager", Name = "Manager",       Description = "Can manage users and view reports",      CreatedAt = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc) },
            new Role { Id = "viewer",  Name = "Viewer",        Description = "Read-only access",                       CreatedAt = new DateTime(2024, 2, 1,  0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
