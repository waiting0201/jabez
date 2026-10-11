using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ShiftScheduleAdjustmentConfiguration : IEntityTypeConfiguration<ShiftScheduleAdjustment>
{
    public void Configure(EntityTypeBuilder<ShiftScheduleAdjustment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Date).HasColumnType("date");
        builder.Property(a => a.RelocatedTo).HasColumnType("date");
        builder.Property(a => a.ActivityTitle).HasMaxLength(200).IsRequired();
        builder.Property(a => a.OriginalDayType).HasMaxLength(20).IsRequired();

        // 本人的通知紀錄，隨使用者一併刪除（CASCADE，故不必加進 UserHandler 的 NO_ACTION 清洗清單）
        builder.HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.ActivityDay)
               .WithMany()
               .HasForeignKey(a => a.ActivityDayId)
               .OnDelete(DeleteBehavior.SetNull);

        // 鈴鐺計數：以 (人, 未讀) 查
        builder.HasIndex(a => new { a.UserId, a.AcknowledgedAt });
    }
}
