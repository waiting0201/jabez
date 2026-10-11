using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ActivityDayConfiguration : IEntityTypeConfiguration<ActivityDay>
{
    public void Configure(EntityTypeBuilder<ActivityDay> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Date).HasColumnType("date");
        builder.Property(a => a.Title).HasMaxLength(200).IsRequired();

        builder.HasOne(a => a.Department)
               .WithMany()
               .HasForeignKey(a => a.DepartmentId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(a => a.Date);
        builder.HasIndex(a => new { a.DepartmentId, a.Date });
    }
}
