using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class LeaveRevocationConfiguration : IEntityTypeConfiguration<LeaveRevocation>
{
    public void Configure(EntityTypeBuilder<LeaveRevocation> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reason)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(r => r.ApprovalStatus)
               .IsRequired()
               .HasMaxLength(20)
               .HasDefaultValue("draft");

        builder.Property(r => r.CurrentStepOrder)
               .HasDefaultValue(1);

        builder.Property(r => r.RevokedHours)
               .HasColumnType("decimal(5,1)");

        builder.Property(r => r.ReviewNote)
               .HasMaxLength(1000);

        // 父單刪除時連帶刪除銷假單（請假單本身僅 draft/returned 可刪，實務上不會有已核准銷假存在）
        builder.HasOne(r => r.LeaveRequest)
               .WithMany(l => l.Revocations)
               .HasForeignKey(r => r.LeaveRequestId)
               .OnDelete(DeleteBehavior.Cascade);

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
               .WithMany(a => a.LeaveRevocations)
               .HasForeignKey(r => r.ApprovalItemId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => new { r.LeaveRequestId, r.ApprovalStatus });
    }
}
