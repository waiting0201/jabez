using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class TravelRequestItemConfiguration : IEntityTypeConfiguration<TravelRequestItem>
{
    public void Configure(EntityTypeBuilder<TravelRequestItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Category)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(i => i.ItemName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(i => i.Quantity)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(i => i.UnitPrice)
               .HasColumnType("decimal(18,2)");

        builder.Property(i => i.TotalPrice)
               .HasColumnType("decimal(18,2)");

        builder.Property(i => i.Note)
               .HasMaxLength(500);

        builder.HasOne(i => i.TravelRequest)
               .WithMany(t => t.Items)
               .HasForeignKey(i => i.TravelRequestId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
