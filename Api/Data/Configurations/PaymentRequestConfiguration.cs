using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class PaymentRequestConfiguration : IEntityTypeConfiguration<PaymentRequest>
{
    public void Configure(EntityTypeBuilder<PaymentRequest> builder)
    {
        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.RequestNo)
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(pr => pr.RequestNo)
               .IsUnique();

        builder.Property(pr => pr.Type)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(pr => pr.ApprovalStatus)
               .IsRequired()
               .HasMaxLength(20)
               .HasDefaultValue("draft");

        builder.Property(pr => pr.CurrentStepOrder)
               .HasDefaultValue(1);

        builder.Property(pr => pr.TotalAmount)
               .HasColumnType("decimal(18,2)");

        builder.Property(pr => pr.ReviewNote)
               .HasMaxLength(1000);

        builder.HasOne(pr => pr.Project)
               .WithMany(p => p.PaymentRequests)
               .HasForeignKey(pr => pr.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pr => pr.Vendor)
               .WithMany(v => v.PaymentRequests)
               .HasForeignKey(pr => pr.VendorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.ApprovalItem)
               .WithMany(a => a.PaymentRequests)
               .HasForeignKey(pr => pr.ApprovalItemId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(pr => pr.SubmittedBy)
               .WithMany()
               .HasForeignKey(pr => pr.SubmittedById)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(pr => pr.ReviewedBy)
               .WithMany()
               .HasForeignKey(pr => pr.ReviewedById)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(pr => pr.PaidBy)
               .WithMany()
               .HasForeignKey(pr => pr.PaidByUserId)
               .OnDelete(DeleteBehavior.NoAction);

        // 無 Seed data — 申請單由使用者操作產生
    }
}
