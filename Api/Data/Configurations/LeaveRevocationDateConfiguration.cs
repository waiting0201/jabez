using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class LeaveRevocationDateConfiguration : IEntityTypeConfiguration<LeaveRevocationDate>
{
    public void Configure(EntityTypeBuilder<LeaveRevocationDate> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Date)
               .HasColumnType("date");

        builder.Property(d => d.Hours)
               .HasColumnType("decimal(4,1)");

        builder.HasOne(d => d.LeaveRevocation)
               .WithMany(r => r.Dates)
               .HasForeignKey(d => d.LeaveRevocationId)
               .OnDelete(DeleteBehavior.Cascade);

        // 同一張銷假單同一天只能出現一次
        builder.HasIndex(d => new { d.LeaveRevocationId, d.Date }).IsUnique();

        // 下游「某日是否已銷假」查詢的走訪路徑
        builder.HasIndex(d => d.Date);
    }
}
