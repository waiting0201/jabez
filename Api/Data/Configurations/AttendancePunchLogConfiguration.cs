using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class AttendancePunchLogConfiguration : IEntityTypeConfiguration<AttendancePunchLog>
{
    public void Configure(EntityTypeBuilder<AttendancePunchLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Action).IsRequired().HasMaxLength(20);
        builder.Property(l => l.BlockReason).HasMaxLength(40);
        builder.Property(l => l.IpAddress).HasMaxLength(64);
        builder.Property(l => l.UserAgent).HasMaxLength(512);
        builder.Property(l => l.ChallengeNonce).HasMaxLength(64);
        builder.Property(l => l.TurnstileResult).HasMaxLength(16);

        builder.HasIndex(l => new { l.UserId, l.AttemptedAt });

        // 挑戰碼單次使用：只約束「成功」列（被擋下的嘗試可能重送同一碼）
        builder.HasIndex(l => l.ChallengeNonce)
               .IsUnique()
               .HasFilter("[Succeeded] = 1 AND [ChallengeNonce] IS NOT NULL");

        // Cascade：刪使用者時一併清除，不必加進 UserHandler 的 NO_ACTION 清洗清單
        builder.HasOne(l => l.User)
               .WithMany()
               .HasForeignKey(l => l.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
