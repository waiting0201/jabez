using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Email).IsRequired().HasMaxLength(256);
        builder.Property(a => a.FailureReason).HasMaxLength(40);
        builder.Property(a => a.IpAddress).HasMaxLength(64);
        builder.Property(a => a.UserAgent).HasMaxLength(512);

        // 鎖定判定：WHERE Email = @e AND AttemptedAt > @since ORDER BY AttemptedAt DESC
        builder.HasIndex(a => new { a.Email, a.AttemptedAt });
        // 稽核查詢（某員工的登入歷史）
        builder.HasIndex(a => new { a.UserId, a.AttemptedAt });
    }
}
