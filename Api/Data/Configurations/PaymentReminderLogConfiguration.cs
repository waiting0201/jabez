using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class PaymentReminderLogConfiguration : IEntityTypeConfiguration<PaymentReminderLog>
{
    public void Configure(EntityTypeBuilder<PaymentReminderLog> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.Property(x => x.BatchId).IsRequired();
        b.Property(x => x.TickedAt).IsRequired();
        b.Property(x => x.TickedAtTaipei).IsRequired();
        b.Property(x => x.ReminderDateTaipei).IsRequired();
        b.Property(x => x.TriggerSource).IsRequired().HasMaxLength(16);
        b.Property(x => x.LineUserIdSnapshot).HasMaxLength(64);
        b.Property(x => x.UserNameSnapshot).HasMaxLength(100);
        b.Property(x => x.Status).IsRequired().HasMaxLength(32);
        b.Property(x => x.ErrorCategory).HasMaxLength(32);
        b.Property(x => x.ErrorMessage).HasMaxLength(500);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        // 主查詢路徑：日期區間 + 結果
        b.HasIndex(x => new { x.ReminderDateTaipei, x.Status })
         .HasDatabaseName("IX_PaymentReminderLogs_Date_Status");

        // 同一次 tick 全部對象
        b.HasIndex(x => x.BatchId)
         .HasDatabaseName("IX_PaymentReminderLogs_BatchId");

        // 同日去重查詢：依使用者 + 日期
        b.HasIndex(x => new { x.FinanceUserId, x.ReminderDateTaipei })
         .HasDatabaseName("IX_PaymentReminderLogs_FinanceUser_Date");

        b.HasOne(x => x.FinanceUser)
         .WithMany()
         .HasForeignKey(x => x.FinanceUserId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TriggeredByUser)
         .WithMany()
         .HasForeignKey(x => x.TriggeredByUserId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
