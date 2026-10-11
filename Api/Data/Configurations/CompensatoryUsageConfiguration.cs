using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class CompensatoryUsageConfiguration : IEntityTypeConfiguration<CompensatoryUsage>
{
    public void Configure(EntityTypeBuilder<CompensatoryUsage> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Hours).HasColumnType("decimal(6,1)");

        builder.HasOne(u => u.Lot)
               .WithMany(l => l.Usages)
               .HasForeignKey(u => u.LotId)
               .OnDelete(DeleteBehavior.Cascade);

        // 補休假單刪除時不連動刪扣抵紀錄（多重級聯路徑），由 Handler 手動清理並回補 RemainingHours
        builder.HasOne(u => u.LeaveRequest)
               .WithMany()
               .HasForeignKey(u => u.LeaveRequestId)
               .OnDelete(DeleteBehavior.NoAction);

        // 「這張補休假吃掉哪幾個 lot」的反查
        builder.HasIndex(u => u.LeaveRequestId);
    }
}
