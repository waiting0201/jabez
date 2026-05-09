using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
{
    public void Configure(EntityTypeBuilder<FamilyMember> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(f => f.Relationship)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(f => f.Occupation)
               .HasMaxLength(100);

        builder.Property(f => f.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.Property(f => f.UpdatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.HasIndex(f => f.UserId);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(f => f.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
