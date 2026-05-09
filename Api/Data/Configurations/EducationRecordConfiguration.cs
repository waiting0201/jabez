using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class EducationRecordConfiguration : IEntityTypeConfiguration<EducationRecord>
{
    public void Configure(EntityTypeBuilder<EducationRecord> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.School)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(e => e.Department)
               .HasMaxLength(200);

        builder.Property(e => e.Degree)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.Property(e => e.UpdatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        // 依 UserId + Order 加複合索引，提升排序查詢效能
        builder.HasIndex(e => new { e.UserId, e.Order });

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(e => e.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
