using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class CompensatoryLotConfiguration : IEntityTypeConfiguration<CompensatoryLot>
{
    public void Configure(EntityTypeBuilder<CompensatoryLot> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.EarnedDate).HasColumnType("date");
        builder.Property(l => l.Hours).HasColumnType("decimal(6,1)");
        builder.Property(l => l.RemainingHours).HasColumnType("decimal(6,1)");
        // 費率為 1.34 / 1.67 / 2.67，兩位小數即足
        builder.Property(l => l.RateSnapshot).HasColumnType("decimal(4,2)");
        builder.Property(l => l.SettledAmount).HasColumnType("decimal(18,2)");

        builder.HasOne(l => l.User)
               .WithMany()
               .HasForeignKey(l => l.UserId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(l => l.SourceOvertimeRequest)
               .WithMany()
               .HasForeignKey(l => l.SourceOvertimeRequestId)
               .OnDelete(DeleteBehavior.NoAction);

        // FIFO 扣抵的走訪路徑：某人尚有剩餘、未到期的 lot 依 EarnedDate 排序
        builder.HasIndex(l => new { l.UserId, l.EarnedDate });

        // 到期結算批次：掃尚未結算且已過期者
        builder.HasIndex(l => new { l.ExpiresAt, l.SettledAt });

        // 一張加班單只開一個 lot（重跑核准時靠此冪等）
        builder.HasIndex(l => l.SourceOvertimeRequestId)
               .IsUnique()
               .HasFilter("[SourceOvertimeRequestId] IS NOT NULL");
    }
}
