using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ApprovalRecordConfiguration : IEntityTypeConfiguration<ApprovalRecord>
{
    public void Configure(EntityTypeBuilder<ApprovalRecord> builder)
    {
        builder.ToTable("ApprovalRecords");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApplicationType).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ReviewNote).HasMaxLength(500);
        builder.Property(x => x.ReviewedAt).HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.Property(x => x.IsEscalated).HasDefaultValue(false);

        // 僅 advance 追加預支會 > 1，其餘申請類型恆為 1（既有資料由 DEFAULT 1 自動相容）
        builder.Property(x => x.RoundNo).HasDefaultValue(1);

        builder.HasOne(x => x.ReviewedBy)
            .WithMany()
            .HasForeignKey(x => x.ReviewedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.OnBehalfOfUser)
            .WithMany()
            .HasForeignKey(x => x.OnBehalfOfUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.ApplicationType, x.ApplicationId, x.StepOrder });
    }
}
