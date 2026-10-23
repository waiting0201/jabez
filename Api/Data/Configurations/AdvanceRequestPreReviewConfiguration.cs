using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class AdvanceRequestPreReviewConfiguration : IEntityTypeConfiguration<AdvanceRequestPreReview>
{
    public void Configure(EntityTypeBuilder<AdvanceRequestPreReview> builder)
    {
        builder.ToTable("AdvanceRequestPreReviews");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RoundNo).HasDefaultValue(1);
        builder.Property(x => x.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        // 預支單刪除 → 關聯列一併刪除
        builder.HasOne(x => x.AdvanceRequest)
               .WithMany(a => a.PreReviews)
               .HasForeignKey(x => x.AdvanceRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        // 預審單被引用中不可刪除（NoAction；PreReviewRequestHandler.DeleteAsync 另有 400 防呆）
        builder.HasOne(x => x.PreReviewRequest)
               .WithMany()
               .HasForeignKey(x => x.PreReviewRequestId)
               .OnDelete(DeleteBehavior.NoAction);

        // 同一張預支單內，同一張預審單只能出現一次（跨批次）
        builder.HasIndex(x => new { x.AdvanceRequestId, x.PreReviewRequestId }).IsUnique();

        // 反查「這張預審單被哪些預支單引用」
        builder.HasIndex(x => x.PreReviewRequestId);
    }
}
