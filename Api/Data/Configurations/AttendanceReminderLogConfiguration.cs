using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class AttendanceReminderLogConfiguration : IEntityTypeConfiguration<AttendanceReminderLog>
{
    public void Configure(EntityTypeBuilder<AttendanceReminderLog> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.Property(x => x.BatchId).IsRequired();
        b.Property(x => x.TickedAt).IsRequired();
        b.Property(x => x.TickedAtTaipei).IsRequired();
        b.Property(x => x.TargetTimeTaipei).IsRequired().HasMaxLength(5);
        b.Property(x => x.ReminderType).IsRequired().HasMaxLength(16);
        b.Property(x => x.TriggerSource).IsRequired().HasMaxLength(16);
        b.Property(x => x.LineUserIdSnapshot).HasMaxLength(64);
        b.Property(x => x.UserNameSnapshot).HasMaxLength(100);
        b.Property(x => x.Status).IsRequired().HasMaxLength(16);
        b.Property(x => x.ErrorCategory).HasMaxLength(32);
        b.Property(x => x.ErrorMessage).HasMaxLength(500);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        // 主查詢路徑：日期區間 + 結果 + 提醒類型
        b.HasIndex(x => new { x.TickedAtTaipei, x.Status, x.ReminderType })
         .HasDatabaseName("IX_AttendanceReminderLogs_TickedAtTaipei_Status_Type");

        // 同一次 tick 全部對象
        b.HasIndex(x => x.BatchId)
         .HasDatabaseName("IX_AttendanceReminderLogs_BatchId");

        // 員工被通知歷史
        b.HasIndex(x => new { x.UserId, x.TickedAtTaipei })
         .HasDatabaseName("IX_AttendanceReminderLogs_UserId_TickedAtTaipei");

        b.HasOne(x => x.User)
         .WithMany()
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TriggeredByUser)
         .WithMany()
         .HasForeignKey(x => x.TriggeredByUserId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
