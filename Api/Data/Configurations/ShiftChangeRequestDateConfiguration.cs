using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ShiftChangeRequestDateConfiguration : IEntityTypeConfiguration<ShiftChangeRequestDate>
{
    public void Configure(EntityTypeBuilder<ShiftChangeRequestDate> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Date).HasColumnType("date");
        builder.Property(d => d.FromDayType).IsRequired().HasMaxLength(20);
        builder.Property(d => d.ToDayType).IsRequired().HasMaxLength(20);

        builder.HasOne(d => d.ShiftChangeRequest)
               .WithMany(r => r.Dates)
               .HasForeignKey(d => d.ShiftChangeRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        // 同一張單同一天只能出現一次
        builder.HasIndex(d => new { d.ShiftChangeRequestId, d.Date }).IsUnique();
    }
}
