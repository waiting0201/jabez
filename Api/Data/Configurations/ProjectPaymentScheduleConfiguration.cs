using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ProjectPaymentScheduleConfiguration : IEntityTypeConfiguration<ProjectPaymentSchedule>
{
    public void Configure(EntityTypeBuilder<ProjectPaymentSchedule> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
               .HasDefaultValueSql("NEWID()");

        builder.Property(s => s.PeriodNo)
               .IsRequired();

        builder.Property(s => s.BillingAmount)
               .HasColumnType("decimal(18,2)");

        builder.Property(s => s.InvoiceAmount)
               .HasColumnType("decimal(18,2)");

        builder.Property(s => s.DepositAmount)
               .HasColumnType("decimal(18,2)");

        builder.Property(s => s.DeductionNote)
               .HasMaxLength(500);

        builder.HasOne(s => s.Project)
               .WithMany(p => p.PaymentSchedules)
               .HasForeignKey(s => s.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.ProjectId, s.PeriodNo });
    }
}
