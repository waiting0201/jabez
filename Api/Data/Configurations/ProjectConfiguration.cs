using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(p => p.Code)
               .IsUnique();

        builder.Property(p => p.Status)
               .IsRequired()
               .HasMaxLength(20)
               .HasDefaultValue("active");

        builder.Property(p => p.GoogleDriveUrl)
               .HasMaxLength(500);

        builder.Property(p => p.BudgetAmount)
               .HasColumnType("decimal(18,2)");

        builder.Property(p => p.ActualAmount)
               .HasColumnType("decimal(18,2)");

        builder.Property(p => p.BusinessAmount)
               .HasColumnType("decimal(18,2)");

        builder.HasOne(p => p.Department)
               .WithMany()
               .HasForeignKey(p => p.DepartmentId)
               .OnDelete(DeleteBehavior.SetNull);

        // Seed data — mirrors Angular MOCK_PROJECTS
        builder.HasData(
            new Project
            {
                Id             = 1,
                Code           = "P2024-001",
                Status         = "closed",
                DepartmentId   = 3,
                BudgetAmount   = 500000m,
                ActualAmount   = 480000m,
                BusinessAmount = 450000m,
                GoogleDriveUrl = "https://drive.google.com/drive/folders/example1",
                CreatedAt      = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            },
            new Project
            {
                Id             = 2,
                Code           = "P2024-002",
                DepartmentId   = 2,
                BudgetAmount   = 1200000m,
                ActualAmount   = 0m,
                BusinessAmount = 1100000m,
                GoogleDriveUrl = "https://drive.google.com/drive/folders/example2",
                CreatedAt      = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            },
            new Project
            {
                Id             = 3,
                Code           = "P2025-001",
                DepartmentId   = 1,
                BudgetAmount   = 300000m,
                ActualAmount   = 280000m,
                BusinessAmount = 250000m,
                GoogleDriveUrl = "https://drive.google.com/drive/folders/example3",
                CreatedAt      = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            }
        );
    }
}
