using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ShiftScheduleDayConfiguration : IEntityTypeConfiguration<ShiftScheduleDay>
{
    public void Configure(EntityTypeBuilder<ShiftScheduleDay> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Date).HasColumnType("date");
        builder.Property(d => d.DayType).HasMaxLength(20).IsRequired();

        builder.HasOne(d => d.User)
               .WithMany()
               .HasForeignKey(d => d.UserId)
               .OnDelete(DeleteBehavior.NoAction);

        // 一人一天一列 —— 整月整批替換時靠此擋住重複寫入
        builder.HasIndex(d => new { d.UserId, d.Date }).IsUnique();

        // 出勤排休總覽表 / 缺勤合併：以日期區間 + 全部員工撈取
        builder.HasIndex(d => d.Date);
    }
}
