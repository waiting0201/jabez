using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.FileName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(i => i.InvoiceNo)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(i => i.Amount)
               .HasColumnType("decimal(18,2)");

        builder.Property(i => i.ItemName)
               .HasMaxLength(200);

        builder.Property(i => i.Note)
               .HasMaxLength(500);

        builder.Property(i => i.FileUrl)
               .HasMaxLength(500);

        builder.HasOne(i => i.PaymentRequest)
               .WithMany(pr => pr.InvoiceItems)
               .HasForeignKey(i => i.PaymentRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        // 無 Seed data — 發票明細由使用者操作產生
    }
}
