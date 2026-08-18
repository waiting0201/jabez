using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class SalaryAdjustmentRecordConfiguration : IEntityTypeConfiguration<SalaryAdjustmentRecord>
{
    public void Configure(EntityTypeBuilder<SalaryAdjustmentRecord> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.BaseSalary)
               .IsRequired()
               .HasColumnType("decimal(18,2)");

        builder.Property(s => s.OtherAllowance)
               .HasColumnType("decimal(18,2)");

        builder.Property(s => s.AdjustmentDifference)
               .HasColumnType("decimal(18,2)");

        builder.Property(s => s.MealAllowance)
               .HasColumnType("decimal(18,2)");

        builder.Property(s => s.TotalAmount)
               .IsRequired()
               .HasColumnType("decimal(18,2)");

        builder.Property(s => s.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.Property(s => s.UpdatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        // 依 UserId + EffectiveDate 加索引，提升「找最新有效薪資」查詢效能
        builder.HasIndex(s => new { s.UserId, s.EffectiveDate });

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(s => s.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
