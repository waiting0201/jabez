using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class RequestDesignatedReviewerConfiguration : IEntityTypeConfiguration<RequestDesignatedReviewer>
{
    public void Configure(EntityTypeBuilder<RequestDesignatedReviewer> builder)
    {
        builder.ToTable("RequestDesignatedReviewers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RequestType).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
        builder.Property(x => x.Comment).HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.HasOne(x => x.Reviewer)
            .WithMany()
            .HasForeignKey(x => x.ReviewerId)
            .OnDelete(DeleteBehavior.NoAction);

        // 複合索引：同一申請單的同一審核者不可重複
        builder.HasIndex(x => new { x.RequestType, x.RequestId, x.ReviewerId })
            .IsUnique();

        // 查詢用索引：快速找到某申請單的所有指定審核者
        builder.HasIndex(x => new { x.RequestType, x.RequestId, x.StepOrder });
    }
}
