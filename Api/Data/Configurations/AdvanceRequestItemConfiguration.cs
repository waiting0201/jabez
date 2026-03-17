using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class AdvanceRequestItemConfiguration : IEntityTypeConfiguration<AdvanceRequestItem>
{
    public void Configure(EntityTypeBuilder<AdvanceRequestItem> builder)
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

        builder.Property(i => i.CashAmount)
               .HasColumnType("decimal(18,2)");

        builder.Property(i => i.CheckAmount)
               .HasColumnType("decimal(18,2)");

        builder.Property(i => i.Note)
               .HasMaxLength(500);

        builder.HasOne(i => i.AdvanceRequest)
               .WithMany(a => a.Items)
               .HasForeignKey(i => i.AdvanceRequestId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
