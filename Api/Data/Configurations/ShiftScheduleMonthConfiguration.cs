using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ShiftScheduleMonthConfiguration : IEntityTypeConfiguration<ShiftScheduleMonth>
{
    public void Configure(EntityTypeBuilder<ShiftScheduleMonth> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Status).HasMaxLength(20).IsRequired().HasDefaultValue("draft");

        builder.HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(m => new { m.UserId, m.Year, m.Month }).IsUnique();

        // 20 號未完成提醒 / 26 號自動排班批次：以年月撈全公司
        builder.HasIndex(m => new { m.Year, m.Month });
    }
}
