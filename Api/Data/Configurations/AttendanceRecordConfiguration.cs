using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.HasKey(a => a.Id);

        // 每人每天只能有一筆打卡紀錄
        builder.HasIndex(a => new { a.UserId, a.RecordDate })
               .IsUnique();

        builder.Property(a => a.RecordDate)
               .IsRequired()
               .HasColumnType("date");

        builder.Property(a => a.IsClockOutAuto)
               .HasDefaultValue(false);

        builder.Property(a => a.IsBusinessTrip)
               .HasDefaultValue(false);

        builder.Property(a => a.Remark)
               .HasMaxLength(500);

        builder.Property(a => a.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Taipei Standard Time'");

        builder.HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.OvertimeRequest)
               .WithMany()
               .HasForeignKey(a => a.OvertimeRequestId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
