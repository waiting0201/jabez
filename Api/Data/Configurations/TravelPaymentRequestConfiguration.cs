using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class TravelPaymentRequestConfiguration : IEntityTypeConfiguration<TravelPaymentRequest>
{
    public void Configure(EntityTypeBuilder<TravelPaymentRequest> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.RequestNo)
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(t => t.RequestNo)
               .IsUnique();

        builder.Property(t => t.Destination)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(t => t.Purpose)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(t => t.GrandTotal)
               .HasColumnType("decimal(18,2)");

        builder.Property(t => t.ApprovalStatus)
               .IsRequired()
               .HasMaxLength(20)
               .HasDefaultValue("draft");

        builder.Property(t => t.CurrentStepOrder)
               .HasDefaultValue(1);

        builder.Property(t => t.ReviewNote)
               .HasMaxLength(1000);

        builder.HasOne(t => t.Employee)
               .WithMany()
               .HasForeignKey(t => t.EmployeeId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.ReviewedBy)
               .WithMany()
               .HasForeignKey(t => t.ReviewedById)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(t => t.PaidBy)
               .WithMany()
               .HasForeignKey(t => t.PaidByUserId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(t => t.ApprovalItem)
               .WithMany(a => a.TravelPaymentRequests)
               .HasForeignKey(t => t.ApprovalItemId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Project)
               .WithMany()
               .HasForeignKey(t => t.ProjectId)
               .OnDelete(DeleteBehavior.SetNull);

        // 無 Seed data — 出差請款申請由使用者操作產生
    }
}
