using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class PreReviewRequestAttachmentConfiguration : IEntityTypeConfiguration<PreReviewRequestAttachment>
{
    public void Configure(EntityTypeBuilder<PreReviewRequestAttachment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(a => a.FileUrl)
               .HasMaxLength(500);

        builder.HasOne(a => a.PreReviewRequest)
               .WithMany(pr => pr.Attachments)
               .HasForeignKey(a => a.PreReviewRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        // 無 Seed data — 附件由使用者操作產生
    }
}
