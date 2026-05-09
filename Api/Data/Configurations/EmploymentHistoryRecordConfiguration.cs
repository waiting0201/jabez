using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class EmploymentHistoryRecordConfiguration : IEntityTypeConfiguration<EmploymentHistoryRecord>
{
    public void Configure(EntityTypeBuilder<EmploymentHistoryRecord> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Organization)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(e => e.JobTitle)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.Property(e => e.UpdatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.HasIndex(e => new { e.UserId, e.Order });

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(e => e.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
