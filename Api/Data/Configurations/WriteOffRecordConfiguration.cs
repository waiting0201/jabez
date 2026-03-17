using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class WriteOffRecordConfiguration : IEntityTypeConfiguration<WriteOffRecord>
{
    public void Configure(EntityTypeBuilder<WriteOffRecord> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.CashTotal)
               .HasColumnType("decimal(18,2)");

        builder.Property(w => w.CheckTotal)
               .HasColumnType("decimal(18,2)");

        builder.Property(w => w.GrandTotal)
               .HasColumnType("decimal(18,2)");

        builder.Property(w => w.Note)
               .HasMaxLength(1000);

        builder.HasOne(w => w.AdvanceRequest)
               .WithMany(a => a.WriteOffs)
               .HasForeignKey(w => w.AdvanceRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.SubmittedBy)
               .WithMany()
               .HasForeignKey(w => w.SubmittedById)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
