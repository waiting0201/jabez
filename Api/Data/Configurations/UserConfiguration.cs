using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(u => u.Email)
               .IsRequired()
               .HasMaxLength(200);

        builder.HasIndex(u => u.Email)
               .IsUnique();

        builder.Property(u => u.PasswordHash)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(u => u.Avatar)
               .HasMaxLength(500);

        builder.Property(u => u.SignatureUrl)
               .HasMaxLength(500);

        builder.Property(u => u.Status)
               .IsRequired()
               .HasMaxLength(20)
               .HasDefaultValue("active");

        builder.Property(u => u.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.Property(u => u.UpdatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.Property(u => u.IsSuperAdmin)
               .HasDefaultValue(false);

        builder.Property(u => u.MustChangePassword)
               .HasDefaultValue(false);

        // Employee fields
        builder.Property(u => u.BaseSalary)
               .HasColumnType("decimal(18,2)");

        builder.HasOne(u => u.Department)
               .WithMany(d => d.Users)
               .HasForeignKey(u => u.DepartmentId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.JobTitle)
               .WithMany(j => j.Users)
               .HasForeignKey(u => u.JobTitleId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.Agent)
               .WithMany()
               .HasForeignKey(u => u.AgentUserId)
               .OnDelete(DeleteBehavior.NoAction);

        // Seed data（加入 Employee 欄位）
        builder.HasData(
            // 超管帳號（隱藏，不出現在使用者管理列表）
            new User
            {
                Id           = new Guid("00000000-0000-0000-0000-000000000001"),
                Name         = "System Admin",
                Email        = "sa@system.local",
                // BCrypt hash of "Admin@123"（正式環境請立即變更）
                PasswordHash = "$2a$11$hBaZunc8xtFIsRVh738SJuHisvnVAsIODyfkzLxjMN.is7jZn3K7e",
                Status       = "active",
                IsSuperAdmin = true,
                CreatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id           = new Guid("11111111-1111-1111-1111-111111111111"),
                Name         = "Alice Chen",
                Email        = "alice@example.com",
                // BCrypt hash of "Admin@123"
                PasswordHash = "$2a$11$hBaZunc8xtFIsRVh738SJuHisvnVAsIODyfkzLxjMN.is7jZn3K7e",
                Status       = "active",
                DepartmentId = 1, // 會計部
                JobTitleId   = 4, // 部門主管
                HireDate     = new DateTime(2023, 12, 28, 0, 0, 0, DateTimeKind.Utc),
                BaseSalary   = 60000m,
                CreatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id           = new Guid("22222222-2222-2222-2222-222222222222"),
                Name         = "Bob Wang",
                Email        = "bob@example.com",
                PasswordHash = "$2a$11$hBaZunc8xtFIsRVh738SJuHisvnVAsIODyfkzLxjMN.is7jZn3K7e",
                Status       = "active",
                DepartmentId = 2, // 財務部
                JobTitleId   = 2, // 資深工程師
                HireDate     = new DateTime(2024, 2, 9, 0, 0, 0, DateTimeKind.Utc),
                BaseSalary   = 60000m,
                CreatedAt    = new DateTime(2024, 2, 10, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt    = new DateTime(2024, 2, 10, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id           = new Guid("33333333-3333-3333-3333-333333333333"),
                Name         = "Carol Liu",
                Email        = "carol@example.com",
                PasswordHash = "$2a$11$hBaZunc8xtFIsRVh738SJuHisvnVAsIODyfkzLxjMN.is7jZn3K7e",
                Status       = "active",
                DepartmentId = 3, // 業務部
                JobTitleId   = 1, // 工程師
                HireDate     = new DateTime(2024, 3, 3, 0, 0, 0, DateTimeKind.Utc),
                BaseSalary   = 50000m,
                CreatedAt    = new DateTime(2024, 3, 5, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt    = new DateTime(2024, 3, 5, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
