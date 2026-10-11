using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class AttendanceAuditLogConfiguration : IEntityTypeConfiguration<AttendanceAuditLog>
{
    public void Configure(EntityTypeBuilder<AttendanceAuditLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.RecordDate).HasColumnType("date");
        builder.Property(l => l.ModifiedByName).IsRequired().HasMaxLength(100);
        builder.Property(l => l.RemarkBefore).HasMaxLength(500);
        builder.Property(l => l.RemarkAfter).HasMaxLength(500);

        builder.HasIndex(l => new { l.AttendanceRecordId, l.ModifiedAt });
        builder.HasIndex(l => new { l.OwnerUserId, l.RecordDate });

        // 只對打卡紀錄設 Cascade；ModifiedById / OwnerUserId 刻意無 FK（見 entity 註解）
        builder.HasOne(l => l.AttendanceRecord)
               .WithMany()
               .HasForeignKey(l => l.AttendanceRecordId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
