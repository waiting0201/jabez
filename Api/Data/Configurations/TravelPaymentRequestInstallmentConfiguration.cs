using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class TravelPaymentRequestInstallmentConfiguration : IEntityTypeConfiguration<TravelPaymentRequestInstallment>
{
    public void Configure(EntityTypeBuilder<TravelPaymentRequestInstallment> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Amount)
               .HasColumnType("decimal(18,2)");

        builder.Property(i => i.Note)
               .HasMaxLength(500);

        builder.HasIndex(i => new { i.TravelPaymentRequestId, i.InstallmentNo })
               .IsUnique();

        builder.HasIndex(i => i.PaidAt);

        builder.HasOne(i => i.TravelPaymentRequest)
               .WithMany(t => t.Installments)
               .HasForeignKey(i => i.TravelPaymentRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.PaidBy)
               .WithMany()
               .HasForeignKey(i => i.PaidByUserId)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
