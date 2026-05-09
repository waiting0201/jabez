using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class RewardPunishmentRecordConfiguration : IEntityTypeConfiguration<RewardPunishmentRecord>
{
    public void Configure(EntityTypeBuilder<RewardPunishmentRecord> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Type)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(r => r.Category)
               .HasMaxLength(100);

        builder.Property(r => r.Count)
               .HasDefaultValue(1);

        builder.Property(r => r.Reason)
               .HasColumnType("nvarchar(max)");

        builder.Property(r => r.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.Property(r => r.UpdatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.HasIndex(r => r.UserId);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
