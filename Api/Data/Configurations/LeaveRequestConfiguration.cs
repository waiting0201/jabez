using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.LeaveType)
               .IsRequired()
               .HasMaxLength(20);

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

        builder.HasOne(l => l.Employee)
               .WithMany()
               .HasForeignKey(l => l.EmployeeId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.ReviewedBy)
               .WithMany()
               .HasForeignKey(l => l.ReviewedById)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(l => l.ApprovalItem)
               .WithMany(a => a.LeaveRequests)
               .HasForeignKey(l => l.ApprovalItemId)
               .OnDelete(DeleteBehavior.SetNull);

        // 無 Seed data — 請假申請由使用者操作產生
    }
}
