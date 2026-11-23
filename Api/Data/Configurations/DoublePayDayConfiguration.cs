using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class DoublePayDayConfiguration : IEntityTypeConfiguration<DoublePayDay>
{
    public void Configure(EntityTypeBuilder<DoublePayDay> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Date)
               .HasColumnType("date")
               .IsRequired();

        builder.HasIndex(d => d.Date)
               .IsUnique();

        builder.Property(d => d.Name)
               .HasMaxLength(50)
               .IsRequired();
    }
}
