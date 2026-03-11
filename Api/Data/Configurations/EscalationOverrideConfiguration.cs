using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class EscalationOverrideConfiguration : IEntityTypeConfiguration<EscalationOverride>
{
    public void Configure(EntityTypeBuilder<EscalationOverride> builder)
    {
        builder.ToTable("EscalationOverrides");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApplicationType).IsRequired().HasMaxLength(30);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.HasOne(x => x.Reviewer)
            .WithMany()
            .HasForeignKey(x => x.ReviewerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.OnBehalfOfUser)
            .WithMany()
            .HasForeignKey(x => x.OnBehalfOfUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.ApplicationType, x.ApplicationId, x.StepOrder })
            .IsUnique();
    }
}
