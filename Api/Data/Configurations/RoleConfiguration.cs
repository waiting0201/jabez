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
        // Seed: 與本機資料庫同步（2026-03-24）
        builder.HasData(
            new Role { Id = "admin",                                    Name = "後端管理者",       Description = "Full system access",                CreatedAt = new DateTime(2024, 1, 1,  0, 0, 0, DateTimeKind.Utc) },
            new Role { Id = "manager",                                  Name = "總管理處",         Description = "Can manage users and view reports", CreatedAt = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc) },
            new Role { Id = "viewer",                                   Name = "一般員工",         Description = "Read-only access",                 CreatedAt = new DateTime(2024, 2, 1,  0, 0, 0, DateTimeKind.Utc) },
            new Role { Id = "3afbfc1e-4caa-4a4e-af1e-ebdc0d9002b4",     Name = "員工-測試",        Description = null,                              CreatedAt = new DateTime(2024, 3, 1,  0, 0, 0, DateTimeKind.Utc) },
            new Role { Id = "44e48f58-1bef-441e-bb70-a624d4f97856",     Name = "協理-測試",        Description = null,                              CreatedAt = new DateTime(2024, 3, 1,  0, 0, 0, DateTimeKind.Utc) },
            new Role { Id = "fe015c41-d9a8-48fa-994d-5588b9c4a92b",     Name = "經理副理主管-測試", Description = null,                              CreatedAt = new DateTime(2024, 3, 1,  0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
