using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.HasKey(l => l.Id);

        // 送簽時才取號（RequestNoGenerator），草稿階段為 null
        builder.Property(l => l.RequestNo)
               .HasMaxLength(50);

        builder.HasIndex(l => l.RequestNo)
               .IsUnique()
               .HasFilter("[RequestNo] IS NOT NULL");

        builder.Property(l => l.LeaveType)
               .IsRequired()
               .HasMaxLength(30);

        builder.Property(l => l.Reason)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(l => l.ApprovalStatus)
               .IsRequired()
               .HasMaxLength(20)
               .HasDefaultValue("draft");

        builder.Property(l => l.CurrentStepOrder)
               .HasDefaultValue(1);

        builder.Property(l => l.Hours)
               .HasColumnType("decimal(5,1)");

        builder.Property(l => l.ReviewNote)
               .HasMaxLength(1000);

        builder.Property(l => l.BereavementRelationship)
               .HasMaxLength(50);

        // 育嬰留停專用欄位（皆可為 null，其餘假別不使用）
        builder.Property(l => l.ChildBirthDate)
               .HasColumnType("date");

        builder.HasOne(l => l.Employee)
               .WithMany()
               .HasForeignKey(l => l.EmployeeId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.ReviewedBy)
               .WithMany()
               .HasForeignKey(l => l.ReviewedById)
               .OnDelete(DeleteBehavior.NoAction);

        // 職務代理人（記錄 + 通知，不參與簽核）；NoAction 避免多重級聯路徑，刪除使用者時由 UserHandler 清洗設 NULL
        builder.HasOne(l => l.AgentUser)
               .WithMany()
               .HasForeignKey(l => l.AgentUserId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(l => l.ApprovalItem)
               .WithMany(a => a.LeaveRequests)
               .HasForeignKey(l => l.ApprovalItemId)
               .OnDelete(DeleteBehavior.SetNull);

        // 無 Seed data — 請假申請由使用者操作產生
    }
}
