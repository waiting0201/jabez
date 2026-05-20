using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class AdvanceRequestInstallmentConfiguration : IEntityTypeConfiguration<AdvanceRequestInstallment>
{
    public void Configure(EntityTypeBuilder<AdvanceRequestInstallment> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Amount)
               .HasColumnType("decimal(18,2)");

        builder.Property(i => i.Note)
               .HasMaxLength(500);

        builder.HasIndex(i => new { i.AdvanceRequestId, i.InstallmentNo })
               .IsUnique();

        builder.HasIndex(i => i.PaidAt);

        builder.HasOne(i => i.AdvanceRequest)
               .WithMany(a => a.Installments)
               .HasForeignKey(i => i.AdvanceRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.PaidBy)
               .WithMany()
               .HasForeignKey(i => i.PaidByUserId)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
