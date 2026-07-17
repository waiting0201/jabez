using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class TravelRequestParticipantDateConfiguration : IEntityTypeConfiguration<TravelRequestParticipantDate>
{
    public void Configure(EntityTypeBuilder<TravelRequestParticipantDate> builder)
    {
        builder.HasKey(d => d.Id);

        builder.HasIndex(d => new { d.TravelRequestParticipantId, d.Date })
               .IsUnique();

        builder.HasOne(d => d.Participant)
               .WithMany(p => p.Dates)
               .HasForeignKey(d => d.TravelRequestParticipantId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
