using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class JobTitleConfiguration : IEntityTypeConfiguration<JobTitle>
{
    public void Configure(EntityTypeBuilder<JobTitle> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(j => j.Level)
               .IsRequired();

        builder.Property(j => j.Description)
               .HasMaxLength(500);

        builder.Property(j => j.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        // Seed: 與本機資料庫同步（2026-03-24）— Level 數字越小 = 層級越高
        builder.HasData(
            new JobTitle { Id = 1,  Name = "工程師",         Level = 5, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new JobTitle { Id = 2,  Name = "專案規劃師/店員", Level = 7, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new JobTitle { Id = 3,  Name = "主任工程師",     Level = 6, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new JobTitle { Id = 4,  Name = "專案副理/店長",   Level = 6, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new JobTitle { Id = 5,  Name = "總監",           Level = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new JobTitle { Id = 6,  Name = "COO",            Level = 2, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new JobTitle { Id = 7,  Name = "CFO",            Level = 2, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new JobTitle { Id = 8,  Name = "協理",           Level = 3, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new JobTitle { Id = 9,  Name = "專案經理",       Level = 5, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new JobTitle { Id = 10, Name = "經理",           Level = 4, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new JobTitle { Id = 11, Name = "會計",           Level = 3, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
