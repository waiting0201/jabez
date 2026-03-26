using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ApprovalItemConfiguration : IEntityTypeConfiguration<ApprovalItem>
{
    public void Configure(EntityTypeBuilder<ApprovalItem> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(a => a.Code)
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(a => a.Code)
               .IsUnique();

        builder.Property(a => a.ApplicationType)
               .HasMaxLength(30);

        // Only one flow per application type
        builder.HasIndex(a => a.ApplicationType)
               .IsUnique()
               .HasFilter("[ApplicationType] IS NOT NULL");

        builder.Property(a => a.Description)
               .HasMaxLength(500);

        builder.Property(a => a.IsActive)
               .HasDefaultValue(true);

        builder.Property(a => a.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.HasData(
            new ApprovalItem { Id = 2, Name = "請款申請", Code = "payment_request",  IsActive = true, ApplicationType = "payment_request", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalItem { Id = 3, Name = "加班申請", Code = "overtime_request",  IsActive = true, ApplicationType = "overtime",         CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalItem { Id = 4, Name = "請假申請", Code = "leave_request",     IsActive = true, ApplicationType = "leave",           CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalItem { Id = 5, Name = "出差申請", Code = "travel_request",    IsActive = true, ApplicationType = "travel",          CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalItem { Id = 6, Name = "預支申請", Code = "advance_request",  IsActive = true, ApplicationType = "advance",         CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalItem { Id = 7, Name = "預支沖銷申請",  Code = "write_off_request",        IsActive = true, ApplicationType = "write_off",        CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalItem { Id = 8, Name = "出差沖銷申請",  Code = "travel_write_off_request",  IsActive = true, ApplicationType = "travel_write_off", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalItem { Id = 9, Name = "假日出差申請",  Code = "holiday_travel_request",    IsActive = true, ApplicationType = "holiday_travel",   CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
