using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class LanguageAbilityConfiguration : IEntityTypeConfiguration<LanguageAbility>
{
    public void Configure(EntityTypeBuilder<LanguageAbility> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Language)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(l => l.Listening)
               .IsRequired()
               .HasMaxLength(10);

        builder.Property(l => l.Speaking)
               .IsRequired()
               .HasMaxLength(10);

        builder.Property(l => l.Reading)
               .IsRequired()
               .HasMaxLength(10);

        builder.Property(l => l.Writing)
               .IsRequired()
               .HasMaxLength(10);

        builder.Property(l => l.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.Property(l => l.UpdatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.HasIndex(l => l.UserId);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(l => l.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
