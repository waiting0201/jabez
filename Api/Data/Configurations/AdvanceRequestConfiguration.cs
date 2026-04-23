using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class AdvanceRequestConfiguration : IEntityTypeConfiguration<AdvanceRequest>
{
    public void Configure(EntityTypeBuilder<AdvanceRequest> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.RequestNo)
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(a => a.RequestNo)
               .IsUnique();

        builder.Property(a => a.ActivityName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(a => a.ActivityPeriod)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(a => a.ApprovalStatus)
               .IsRequired()
               .HasMaxLength(20)
               .HasDefaultValue("draft");

        builder.Property(a => a.CurrentStepOrder)
               .HasDefaultValue(1);

        builder.Property(a => a.CashTotal)
               .HasColumnType("decimal(18,2)");

        builder.Property(a => a.CheckTotal)
               .HasColumnType("decimal(18,2)");

        builder.Property(a => a.GrandTotal)
               .HasColumnType("decimal(18,2)");

        builder.Property(a => a.ReviewNote)
               .HasMaxLength(1000);

        builder.HasOne(a => a.Project)
               .WithMany(p => p.AdvanceRequests)
               .HasForeignKey(a => a.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.ApprovalItem)
               .WithMany(ai => ai.AdvanceRequests)
               .HasForeignKey(a => a.ApprovalItemId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.SubmittedBy)
               .WithMany()
               .HasForeignKey(a => a.SubmittedById)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.ReviewedBy)
               .WithMany()
               .HasForeignKey(a => a.ReviewedById)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(a => a.PaidBy)
               .WithMany()
               .HasForeignKey(a => a.PaidByUserId)
               .OnDelete(DeleteBehavior.NoAction);

        // 結案欄位
        builder.Property(a => a.IsClosed)
               .HasDefaultValue(false);

        builder.HasOne(a => a.ClosedBy)
               .WithMany()
               .HasForeignKey(a => a.ClosedById)
               .OnDelete(DeleteBehavior.NoAction);

        // 退還差額
        builder.Property(a => a.RefundAmount)
               .HasColumnType("decimal(18,2)");

        builder.Property(a => a.RefundedAmount)
               .HasColumnType("decimal(18,2)");

        builder.HasOne(a => a.RefundedBy)
               .WithMany()
               .HasForeignKey(a => a.RefundedByUserId)
               .OnDelete(DeleteBehavior.NoAction);

    }
}
