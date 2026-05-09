using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class HealthInsuranceDependentConfiguration : IEntityTypeConfiguration<HealthInsuranceDependent>
{
    public void Configure(EntityTypeBuilder<HealthInsuranceDependent> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(h => h.Relationship)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(h => h.IdNumber)
               .HasMaxLength(20);

        builder.Property(h => h.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.Property(h => h.UpdatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.HasIndex(h => h.UserId);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(h => h.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
