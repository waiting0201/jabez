using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class TravelRequestParticipantConfiguration : IEntityTypeConfiguration<TravelRequestParticipant>
{
    public void Configure(EntityTypeBuilder<TravelRequestParticipant> builder)
    {
        builder.HasKey(p => p.Id);

        // 半天以 0.5 計，故為 decimal（92 天上限，decimal(5,1) 足夠）
        builder.Property(p => p.HolidayDays)
               .HasColumnType("decimal(5,1)");

        builder.HasIndex(p => new { p.TravelRequestId, p.UserId })
               .IsUnique();

        builder.HasOne(p => p.TravelRequest)
               .WithMany(t => t.Participants)
               .HasForeignKey(p => p.TravelRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.User)
               .WithMany()
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
