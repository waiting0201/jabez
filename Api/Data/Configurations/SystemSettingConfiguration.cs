using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.HasKey(s => s.Id);

        // ── 站台設定 ─────────────────────────────────────────
        builder.Property(s => s.SiteName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.SiteUrl).IsRequired().HasMaxLength(500);
        builder.Property(s => s.ContactEmail).IsRequired().HasMaxLength(200);
        builder.Property(s => s.SiteDescription).HasMaxLength(1000);
        builder.Property(s => s.Language).IsRequired().HasMaxLength(20);
        builder.Property(s => s.Timezone).IsRequired().HasMaxLength(100);
        builder.Property(s => s.MaintenanceMessage).HasMaxLength(1000);

        // ── 工時設定 ─────────────────────────────────────────
        builder.Property(s => s.WorkStartTime).IsRequired().HasMaxLength(5);   // "HH:MM"
        builder.Property(s => s.WorkEndTime).IsRequired().HasMaxLength(5);     // "HH:MM"

        // ── Seed：Id = 1 ────────────────────────────────────
        builder.HasData(new SystemSetting
        {
            Id                       = 1,
            SiteName                 = "Jabez Admin",
            SiteUrl                  = "https://admin.jabez.com",
            ContactEmail             = "admin@jabez.com",
            SiteDescription          = "Enterprise administration portal",
            Language                 = "zh-TW",
            Timezone                 = "Asia/Taipei",
            SessionTimeoutMinutes    = 60,
            AllowRegistration        = false,
            RequireEmailVerification = true,
            MaintenanceMode          = false,
            MaintenanceMessage       = "System is under maintenance. Please try again later.",
            WorkStartTime            = "09:00",
            WorkEndTime              = "18:00",
            MonthlyOvertimeLimit     = 46,
        });
    }
}
