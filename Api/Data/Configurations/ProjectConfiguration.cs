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

        builder.Property(p => p.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(p => p.StartDate)
               .IsRequired();

        builder.Property(p => p.Status)
               .IsRequired()
               .HasMaxLength(20)
               .HasDefaultValue("active");

        builder.Property(p => p.GoogleDriveUrl)
               .HasMaxLength(500);

        builder.Property(p => p.ContractAmount)
               .HasColumnType("decimal(18,2)");

        builder.Property(p => p.BusinessAmount)
               .HasColumnType("decimal(18,2)");

        builder.Property(p => p.RemainingAmount)
               .HasColumnType("decimal(18,2)");

        builder.Property(p => p.DepartmentId)
               .IsRequired();

        builder.HasOne(p => p.Department)
               .WithMany()
               .HasForeignKey(p => p.DepartmentId)
               .OnDelete(DeleteBehavior.Restrict);

        // Seed data — mirrors Angular MOCK_PROJECTS
        // Seed: 與本機資料庫同步（2026-03-24）
        builder.HasData(
            new Project
            {
                Id = 1, Code = "P2024-001", Name = "2024年度行銷專案", Status = "closed",
                StartDate = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                DepartmentId = 3, ContractAmount = 480000m, BusinessAmount = 450000m,
                GoogleDriveUrl = "https://drive.google.com/drive/folders/example1",
                CreatedAt = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            },
            new Project
            {
                Id = 2, Code = "P2024-002", Name = "系統升級專案", Status = "active",
                StartDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
                DepartmentId = 2, ContractAmount = 0m, BusinessAmount = 1100000m,
                GoogleDriveUrl = "https://drive.google.com/drive/folders/example2",
                CreatedAt = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            },
            new Project
            {
                Id = 3, Code = "P2025-001", Name = "2025年度研發專案", Status = "active",
                StartDate = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                DepartmentId = 1, ContractAmount = 280000m, BusinessAmount = 250000m,
                GoogleDriveUrl = "https://drive.google.com/drive/folders/example3",
                CreatedAt = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            },
            new Project
            {
                Id = 4, Code = "J11203-T", Name = "壯圍沙丘生態園區出租案", Status = "active",
                DepartmentId = 9, ContractAmount = 2350000m, BusinessAmount = 1410000m,
                GoogleDriveUrl = "test",
                CreatedAt = new DateTime(2026, 3, 17, 0, 0, 0, DateTimeKind.Utc),
            },
            new Project
            {
                Id = 5, Code = "J11405-T", Name = "114-115年梨山風景區部落觀光產業輔導計畫", Status = "active",
                DepartmentId = 8, ContractAmount = 4985000m, BusinessAmount = 2991000m,
                GoogleDriveUrl = "test",
                CreatedAt = new DateTime(2026, 3, 17, 0, 0, 0, DateTimeKind.Utc),
            },
            new Project
            {
                Id = 6, Code = "J11418-T", Name = "鰲鼓濕地森林園區生態旅遊培力與活動推展委託專業服務案", Status = "active",
                StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DepartmentId = 8, ContractAmount = 2258000m, BusinessAmount = 1354800m,
                GoogleDriveUrl = "test",
                CreatedAt = new DateTime(2026, 3, 17, 0, 0, 0, DateTimeKind.Utc),
            },
            new Project
            {
                Id = 7, Code = "J11501-T", Name = "「115年地方創生東區輔導中心」委託辦理計畫案", Status = "closed",
                StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DepartmentId = 8, ContractAmount = 6558000m, BusinessAmount = 3934800m,
                GoogleDriveUrl = "test",
                CreatedAt = new DateTime(2026, 3, 17, 0, 0, 0, DateTimeKind.Utc),
            },
            new Project
            {
                Id = 8, Code = "J11501-TT", Name = "「115年地方創生東區輔導中心」委託辦理計畫案", Status = "active",
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DepartmentId = 8, ContractAmount = 6220000m, BusinessAmount = 3732000m,
                GoogleDriveUrl = "----",
                CreatedAt = new DateTime(2026, 3, 18, 0, 0, 0, DateTimeKind.Utc),
            }
        );
    }
}
