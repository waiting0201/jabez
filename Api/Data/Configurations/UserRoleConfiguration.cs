using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.HasOne(ur => ur.User)
               .WithMany(u => u.UserRoles)
               .HasForeignKey(ur => ur.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
               .WithMany(r => r.UserRoles)
               .HasForeignKey(ur => ur.RoleId)
               .OnDelete(DeleteBehavior.Cascade);

        // Seed data
        builder.HasData(
            new UserRole { UserId = new Guid("11111111-1111-1111-1111-111111111111"), RoleId = "admin"   },
            new UserRole { UserId = new Guid("22222222-2222-2222-2222-222222222222"), RoleId = "manager" },
            new UserRole { UserId = new Guid("33333333-3333-3333-3333-333333333333"), RoleId = "viewer"  }
        );
    }
}
