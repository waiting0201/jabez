using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class AttendanceProjectAllocationConfiguration : IEntityTypeConfiguration<AttendanceProjectAllocation>
{
    public void Configure(EntityTypeBuilder<AttendanceProjectAllocation> builder)
    {
        builder.ToTable("AttendanceProjectAllocations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Percentage)
               .HasColumnType("decimal(5,2)");

        // 打卡紀錄被刪（含刪使用者連帶）時分配一併刪除
        builder.HasOne(x => x.AttendanceRecord)
               .WithMany(a => a.ProjectAllocations)
               .HasForeignKey(x => x.AttendanceRecordId)
               .OnDelete(DeleteBehavior.Cascade);

        // 雙 FK 子表的第二個主檔一律 NoAction（同 OvertimeRequestProject）；
        // 刪除專案時由 ProjectHandler.DeleteAsync 阻擋（清洗會讓歷史工時憑空消失）。
        builder.HasOne(x => x.Project)
               .WithMany()
               .HasForeignKey(x => x.ProjectId)
               .OnDelete(DeleteBehavior.NoAction);

        // 同一筆打卡紀錄同一專案只能有一列
        builder.HasIndex(x => new { x.AttendanceRecordId, x.ProjectId }).IsUnique();

        // ProjectHandler 刪除阻擋與工時總表 projectId 篩選以 ProjectId 反查
        builder.HasIndex(x => x.ProjectId);
    }
}
