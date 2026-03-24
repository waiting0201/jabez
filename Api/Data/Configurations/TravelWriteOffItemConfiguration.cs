using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class TravelWriteOffItemConfiguration : IEntityTypeConfiguration<TravelWriteOffItem>
{
    public void Configure(EntityTypeBuilder<TravelWriteOffItem> builder)
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

        builder.Property(i => i.InvoiceNo)
               .HasMaxLength(50);

        builder.Property(i => i.FileName)
               .HasMaxLength(200);

        builder.Property(i => i.FileUrl)
               .HasMaxLength(500);

        builder.HasOne(i => i.TravelWriteOffRecord)
               .WithMany(w => w.Items)
               .HasForeignKey(i => i.TravelWriteOffRecordId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
