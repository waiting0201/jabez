using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class OvertimeRequestConfiguration : IEntityTypeConfiguration<OvertimeRequest>
{
    public void Configure(EntityTypeBuilder<OvertimeRequest> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Reason)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(o => o.ApprovalStatus)
               .IsRequired()
               .HasMaxLength(20)
               .HasDefaultValue("draft");

        builder.Property(o => o.CurrentStepOrder)
               .HasDefaultValue(1);

        builder.Property(o => o.EstimatedHours)
               .HasColumnType("decimal(5,1)");

        builder.Property(o => o.ReviewNote)
               .HasMaxLength(1000);

        builder.Property(o => o.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.HasOne(o => o.Employee)
               .WithMany()
               .HasForeignKey(o => o.EmployeeId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.ReviewedBy)
               .WithMany()
               .HasForeignKey(o => o.ReviewedById)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(o => o.DesignatedReviewer)
               .WithMany()
               .HasForeignKey(o => o.DesignatedReviewerId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(o => o.ApprovalItem)
               .WithMany(a => a.OvertimeRequests)
               .HasForeignKey(o => o.ApprovalItemId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.Property(o => o.ProjectIds)
               .HasMaxLength(200);

        // 無 Seed data — 加班申請由使用者操作產生
    }
}
