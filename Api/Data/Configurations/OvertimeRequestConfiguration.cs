using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class OvertimeRequestConfiguration : IEntityTypeConfiguration<OvertimeRequest>
{
    public void Configure(EntityTypeBuilder<OvertimeRequest> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Reason)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(o => o.ApprovalStatus)
               .IsRequired()
               .HasMaxLength(20)
               .HasDefaultValue("draft");

        builder.Property(o => o.CurrentStepOrder)
               .HasDefaultValue(1);

        builder.Property(o => o.EstimatedHours)
               .HasColumnType("decimal(5,1)");

        // 補償方式（補休 / 加班費，整單二擇一）。預設 compensatory 同時是舊資料的 backfill 值，
        // 讓上線前所有已核准加班單原封不動留在補休池（見 LeaveRequestHandler.ComputeCompensatoryAsync）。
        builder.Property(o => o.CompensationType)
               .IsRequired()
               .HasMaxLength(20)
               .HasDefaultValue("compensatory");

        // 加班費快照。金額 / 時薪沿用專案金額欄慣例 decimal(18,2)，計酬時數與 EstimatedHours 同精度。
        builder.Property(o => o.OvertimePayAmount)
               .HasColumnType("decimal(18,2)");

        builder.Property(o => o.HourlyRateSnapshot)
               .HasColumnType("decimal(18,2)");

        builder.Property(o => o.PayableHours)
               .HasColumnType("decimal(5,1)");

        builder.Property(o => o.ReviewNote)
               .HasMaxLength(1000);

        builder.Property(o => o.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.HasOne(o => o.Employee)
               .WithMany()
               .HasForeignKey(o => o.EmployeeId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.ReviewedBy)
               .WithMany()
               .HasForeignKey(o => o.ReviewedById)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(o => o.ApprovalItem)
               .WithMany(a => a.OvertimeRequests)
               .HasForeignKey(o => o.ApprovalItemId)
               .OnDelete(DeleteBehavior.SetNull);

        // 關聯專案改由 OvertimeRequestProject 子表表達（原 CSV 欄位 ProjectIds 已 DROP）

        // 無 Seed data — 加班申請由使用者操作產生
    }
}
