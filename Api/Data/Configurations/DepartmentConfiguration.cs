using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(d => d.Code)
               .HasMaxLength(50);

        builder.HasIndex(d => d.Code)
               .IsUnique()
               .HasFilter("[Code] IS NOT NULL");

        builder.Property(d => d.Description)
               .HasMaxLength(500);

        builder.Property(d => d.SortOrder)
               .HasDefaultValue(0);

        builder.Property(d => d.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        // 自我參照（母部門）
        builder.HasOne(d => d.Parent)
               .WithMany(d => d.Children)
               .HasForeignKey(d => d.ParentId)
               .OnDelete(DeleteBehavior.Restrict);

        // Seed: 與本機資料庫同步（2026-03-24）
        builder.HasData(
            new Department { Id = 1,  Name = "會計部",           Code = "AC",             ParentId = 2,    SortOrder = 3, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { Id = 2,  Name = "行政財務部",       Code = "FIN",            ParentId = 5,    SortOrder = 2, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { Id = 3,  Name = "疆界",             Code = "Borders Design", ParentId = 1,    SortOrder = 3, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { Id = 4,  Name = "總監室",           Code = "CEO",            ParentId = null, SortOrder = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { Id = 5,  Name = "雅比斯總公司管理部", Code = "Jabez HQ",      ParentId = 4,    SortOrder = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { Id = 6,  Name = "豐濱營業所",       Code = "Store",          ParentId = 3,    SortOrder = 4, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { Id = 7,  Name = "海銀行",           Code = "SeaStore",       ParentId = 3,    SortOrder = 4, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { Id = 8,  Name = "雅比斯專案",       Code = "Jabez project",  ParentId = 1,    SortOrder = 3, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { Id = 9,  Name = "壯圍營業所",       Code = "YilanStore",     ParentId = 8,    SortOrder = 4, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { Id = 10, Name = "東發號",           Code = "EastStore",      ParentId = 3,    SortOrder = 4, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
