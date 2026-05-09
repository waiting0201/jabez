using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ProfessionalTrainingConfiguration : IEntityTypeConfiguration<ProfessionalTraining>
{
    public void Configure(EntityTypeBuilder<ProfessionalTraining> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.TrainingName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(p => p.TrainingOrg)
               .HasMaxLength(200);

        builder.Property(p => p.Hours)
               .HasColumnType("decimal(18,2)");

        builder.Property(p => p.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.Property(p => p.UpdatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.HasIndex(p => p.UserId);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
