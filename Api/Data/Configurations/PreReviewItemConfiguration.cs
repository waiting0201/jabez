using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class PreReviewItemConfiguration : IEntityTypeConfiguration<PreReviewItem>
{
    public void Configure(EntityTypeBuilder<PreReviewItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.FileName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(i => i.ItemCategory)
               .HasMaxLength(100);

        builder.Property(i => i.Amount)
               .HasColumnType("decimal(18,2)");

        builder.Property(i => i.ItemName)
               .HasMaxLength(200);

        builder.Property(i => i.Description)
               .HasMaxLength(500);

        builder.Property(i => i.Note)
               .HasMaxLength(500);

        builder.Property(i => i.FileUrl)
               .HasMaxLength(500);

        builder.HasOne(i => i.PreReviewRequest)
               .WithMany(pr => pr.Items)
               .HasForeignKey(i => i.PreReviewRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        // 無 Seed data — 品項明細由使用者操作產生
    }
}
