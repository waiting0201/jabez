using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class GeminiUsageLogConfiguration : IEntityTypeConfiguration<GeminiUsageLog>
{
    public void Configure(EntityTypeBuilder<GeminiUsageLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Source).IsRequired().HasMaxLength(20);
        builder.Property(l => l.Model).IsRequired().HasMaxLength(64);

        builder.HasIndex(l => l.CreatedAt);
    }
}
