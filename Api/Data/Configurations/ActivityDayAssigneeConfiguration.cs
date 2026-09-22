using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ActivityDayAssigneeConfiguration : IEntityTypeConfiguration<ActivityDayAssignee>
{
    public void Configure(EntityTypeBuilder<ActivityDayAssignee> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.ActivityDay)
               .WithMany(d => d.Assignees)
               .HasForeignKey(a => a.ActivityDayId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.NoAction);

        // 同一活動日同一人不可重複列入
        builder.HasIndex(a => new { a.ActivityDayId, a.UserId }).IsUnique();

        // 打卡解鎖判定：以 (人, 日) 反查
        builder.HasIndex(a => a.UserId);
    }
}
