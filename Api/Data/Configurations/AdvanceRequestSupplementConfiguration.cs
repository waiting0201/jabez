using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class AdvanceRequestSupplementConfiguration : IEntityTypeConfiguration<AdvanceRequestSupplement>
{
    public void Configure(EntityTypeBuilder<AdvanceRequestSupplement> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Reason)
               .HasMaxLength(500);

        builder.Property(s => s.PrevReviewNote)
               .HasMaxLength(1000);

        builder.HasIndex(s => new { s.AdvanceRequestId, s.RoundNo })
               .IsUnique();

        builder.HasOne(s => s.AdvanceRequest)
               .WithMany(a => a.Supplements)
               .HasForeignKey(s => s.AdvanceRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.CreatedBy)
               .WithMany()
               .HasForeignKey(s => s.CreatedById)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.PrevReviewedBy)
               .WithMany()
               .HasForeignKey(s => s.PrevReviewedById)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
