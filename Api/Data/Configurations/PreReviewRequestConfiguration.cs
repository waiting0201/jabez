using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class PreReviewRequestConfiguration : IEntityTypeConfiguration<PreReviewRequest>
{
    public void Configure(EntityTypeBuilder<PreReviewRequest> builder)
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

        builder.Property(pr => pr.TaxAmount)
               .HasColumnType("decimal(18,2)")
               .HasDefaultValue(0m);

        builder.Property(pr => pr.ReviewNote)
               .HasMaxLength(1000);

        builder.HasOne(pr => pr.Project)
               .WithMany()
               .HasForeignKey(pr => pr.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pr => pr.Vendor)
               .WithMany()
               .HasForeignKey(pr => pr.VendorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.ApprovalItem)
               .WithMany()
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

        // 無 Seed data — 申請單由使用者操作產生
    }
}
