using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ShiftChangeRequestConfiguration : IEntityTypeConfiguration<ShiftChangeRequest>
{
    public void Configure(EntityTypeBuilder<ShiftChangeRequest> builder)
    {
        builder.HasKey(r => r.Id);

        // 送簽時才取號（RequestNoGenerator），草稿階段為 null
        builder.Property(r => r.RequestNo).HasMaxLength(50);
        builder.HasIndex(r => r.RequestNo)
               .IsUnique()
               .HasFilter("[RequestNo] IS NOT NULL");

        builder.Property(r => r.Reason).IsRequired().HasMaxLength(500);

        builder.Property(r => r.ApprovalStatus)
               .IsRequired().HasMaxLength(20).HasDefaultValue("draft");

        builder.Property(r => r.CurrentStepOrder).HasDefaultValue(1);
        builder.Property(r => r.ReviewNote).HasMaxLength(1000);

        // NoAction 避免多重級聯路徑；刪除使用者時由 UserHandler 清洗
        builder.HasOne(r => r.Employee)
               .WithMany()
               .HasForeignKey(r => r.EmployeeId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.ReviewedBy)
               .WithMany()
               .HasForeignKey(r => r.ReviewedById)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.ApprovalItem)
               .WithMany()
               .HasForeignKey(r => r.ApprovalItemId)
               .OnDelete(DeleteBehavior.SetNull);

        // 清單與「該員該月是否有進行中的改班申請」查詢
        builder.HasIndex(r => new { r.EmployeeId, r.Year, r.Month, r.ApprovalStatus });
    }
}
