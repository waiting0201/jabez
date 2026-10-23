using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class DisasterDayOffConfiguration : IEntityTypeConfiguration<DisasterDayOff>
{
    public void Configure(EntityTypeBuilder<DisasterDayOff> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.Date).HasColumnType("date").IsRequired();
        b.Property(x => x.County).IsRequired().HasMaxLength(3);
        b.Property(x => x.Segment).IsRequired().HasMaxLength(8);
        b.Property(x => x.IsPaid).HasDefaultValue(true);
        b.Property(x => x.Source).IsRequired().HasMaxLength(16);
        b.Property(x => x.SourceText).HasMaxLength(1000);
        b.Property(x => x.Status).IsRequired().HasMaxLength(16);
        b.Property(x => x.CreatedByName).HasMaxLength(100);
        b.Property(x => x.CancelledByName).HasMaxLength(100);

        // 同日同縣市同時只能有一筆「有效」紀錄；取消過的列保留（自動抓取據此不再重建）
        b.HasIndex(x => new { x.Date, x.County })
         .IsUnique()
         .HasFilter("[Status] = 'active'")
         .HasDatabaseName("UX_DisasterDayOffs_Date_County_Active");

        // 主查詢路徑：日期區間
        b.HasIndex(x => new { x.Date, x.Status })
         .HasDatabaseName("IX_DisasterDayOffs_Date_Status");
    }
}

public class DisasterDayOffFetchLogConfiguration : IEntityTypeConfiguration<DisasterDayOffFetchLog>
{
    public void Configure(EntityTypeBuilder<DisasterDayOffFetchLog> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.TriggerSource).IsRequired().HasMaxLength(16);
        b.Property(x => x.Summary).IsRequired().HasMaxLength(1000);
        b.HasIndex(x => x.FetchedAt).HasDatabaseName("IX_DisasterDayOffFetchLogs_FetchedAt");
    }
}
