using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class CalendarDayConfiguration : IEntityTypeConfiguration<CalendarDay>
{
    public void Configure(EntityTypeBuilder<CalendarDay> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Date)
               .IsRequired();

        builder.HasIndex(c => c.Date)
               .IsUnique();

        builder.HasIndex(c => new { c.Year, c.IsHoliday });

        builder.Property(c => c.IsHoliday)
               .HasDefaultValue(false);

        builder.Property(c => c.Description)
               .HasMaxLength(100)
               .HasDefaultValue("");

        builder.Property(c => c.Year)
               .IsRequired();
    }
}
