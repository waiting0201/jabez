using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class TravelRequestInstallmentConfiguration : IEntityTypeConfiguration<TravelRequestInstallment>
{
    public void Configure(EntityTypeBuilder<TravelRequestInstallment> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Amount)
               .HasColumnType("decimal(18,2)");

        builder.Property(i => i.Note)
               .HasMaxLength(500);

        builder.HasIndex(i => new { i.TravelRequestId, i.InstallmentNo })
               .IsUnique();

        builder.HasIndex(i => i.PaidAt);

        builder.HasOne(i => i.TravelRequest)
               .WithMany(t => t.Installments)
               .HasForeignKey(i => i.TravelRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.PaidBy)
               .WithMany()
               .HasForeignKey(i => i.PaidByUserId)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
