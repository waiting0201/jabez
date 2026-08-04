using Jabez.Api.Common;
using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class TravelRequestParticipantDateConfiguration : IEntityTypeConfiguration<TravelRequestParticipantDate>
{
    public void Configure(EntityTypeBuilder<TravelRequestParticipantDate> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Slot)
               .HasMaxLength(4)
               .IsRequired()
               .HasDefaultValue(ParticipantDateSlots.Full);

        // 唯一鍵刻意不含 Slot：一天恆為一列，Slot 是該列屬性（同日不可同時上午 + 下午）
        builder.HasIndex(d => new { d.TravelRequestParticipantId, d.Date })
               .IsUnique();

        builder.HasOne(d => d.Participant)
               .WithMany(p => p.Dates)
               .HasForeignKey(d => d.TravelRequestParticipantId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
