using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class WriteOffInstallmentConfiguration : IEntityTypeConfiguration<WriteOffInstallment>
{
    public void Configure(EntityTypeBuilder<WriteOffInstallment> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Amount)
               .HasColumnType("decimal(18,2)");

        builder.Property(i => i.Note)
               .HasMaxLength(500);

        builder.HasIndex(i => new { i.WriteOffRecordId, i.InstallmentNo })
               .IsUnique();

        builder.HasIndex(i => i.PaidAt);

        builder.HasOne(i => i.WriteOffRecord)
               .WithMany(w => w.Installments)
               .HasForeignKey(i => i.WriteOffRecordId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.PaidBy)
               .WithMany()
               .HasForeignKey(i => i.PaidByUserId)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
