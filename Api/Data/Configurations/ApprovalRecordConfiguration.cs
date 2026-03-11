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
