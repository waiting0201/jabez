using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class PayrollAdjustmentConfiguration : IEntityTypeConfiguration<PayrollAdjustment>
{
    public void Configure(EntityTypeBuilder<PayrollAdjustment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.EmployeeId).IsRequired();
        builder.Property(p => p.Year).IsRequired();
        builder.Property(p => p.Month).IsRequired();
        builder.Property(p => p.OtherAddition).HasColumnType("decimal(12,2)").HasDefaultValue(0m);
        builder.Property(p => p.OtherAdditionNote).HasMaxLength(500);
        builder.Property(p => p.OtherDeduction).HasColumnType("decimal(12,2)").HasDefaultValue(0m);
        builder.Property(p => p.OtherDeductionNote).HasMaxLength(500);
        builder.Property(p => p.Note).HasMaxLength(1000);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        // 每位員工每月只能有一筆
        builder.HasIndex(p => new { p.EmployeeId, p.Year, p.Month }).IsUnique();

        builder.HasOne(p => p.Employee)
               .WithMany()
               .HasForeignKey(p => p.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
