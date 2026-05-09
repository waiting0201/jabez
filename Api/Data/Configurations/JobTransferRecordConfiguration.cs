using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class JobTransferRecordConfiguration : IEntityTypeConfiguration<JobTransferRecord>
{
    public void Configure(EntityTypeBuilder<JobTransferRecord> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.FromDepartment)
               .HasMaxLength(100);

        builder.Property(j => j.ToDepartment)
               .HasMaxLength(100);

        builder.Property(j => j.FromJobTitle)
               .HasMaxLength(100);

        builder.Property(j => j.ToJobTitle)
               .HasMaxLength(100);

        builder.Property(j => j.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.Property(j => j.UpdatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.HasIndex(j => j.UserId);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(j => j.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
