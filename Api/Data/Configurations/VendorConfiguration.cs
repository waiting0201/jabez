using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(v => v.TaxId)
               .HasMaxLength(20);

        builder.Property(v => v.Phone)
               .HasMaxLength(50);

        builder.Property(v => v.ContactPerson)
               .HasMaxLength(100);

        builder.Property(v => v.Address)
               .HasMaxLength(500);

        builder.Property(v => v.BankAccount)
               .HasMaxLength(100);

        builder.Property(v => v.BankBookImageUrl)
               .HasMaxLength(500);

        builder.Property(v => v.Note)
               .HasMaxLength(500);

        builder.Property(v => v.IsActive)
               .HasDefaultValue(true);

        builder.Property(v => v.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        // 統編 unique（允許 NULL，但非 NULL 時必須唯一）
        builder.HasIndex(v => v.TaxId)
               .IsUnique()
               .HasFilter("[TaxId] IS NOT NULL");
    }
}
