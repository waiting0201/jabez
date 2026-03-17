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

        builder.HasData(
            new JobTitle { Id = 1, Name = "工程師",     Level = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new JobTitle { Id = 2, Name = "資深工程師", Level = 2, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new JobTitle { Id = 3, Name = "主任工程師", Level = 3, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new JobTitle { Id = 4, Name = "部門主管",   Level = 4, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new JobTitle { Id = 5, Name = "總監",       Level = 5, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
