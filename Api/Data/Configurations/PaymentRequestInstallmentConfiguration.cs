using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class PaymentRequestInstallmentConfiguration : IEntityTypeConfiguration<PaymentRequestInstallment>
{
    public void Configure(EntityTypeBuilder<PaymentRequestInstallment> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Amount)
               .HasColumnType("decimal(18,2)");

        builder.Property(i => i.Note)
               .HasMaxLength(500);

        builder.HasIndex(i => new { i.PaymentRequestId, i.InstallmentNo })
               .IsUnique();

        builder.HasIndex(i => i.PaidAt);

        builder.HasOne(i => i.PaymentRequest)
               .WithMany(p => p.Installments)
               .HasForeignKey(i => i.PaymentRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.PaidBy)
               .WithMany()
               .HasForeignKey(i => i.PaidByUserId)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
