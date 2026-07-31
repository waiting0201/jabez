using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ApprovalStepDesignatedJobTitleConfiguration : IEntityTypeConfiguration<ApprovalStepDesignatedJobTitle>
{
    public void Configure(EntityTypeBuilder<ApprovalStepDesignatedJobTitle> builder)
    {
        builder.ToTable("ApprovalStepDesignatedJobTitles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.HasOne(x => x.ApprovalStep)
               .WithMany(s => s.DesignatedJobTitles)
               .HasForeignKey(x => x.ApprovalStepId)
               .OnDelete(DeleteBehavior.Cascade);

        // 沿用 ApprovalStepException 慣例：第二個 FK 一律 NoAction，避免多重級聯路徑
        // （ApprovalStep.JobTitleId 已是 SetNull，兩邊 Cascade 會觸發 SQL Server 1785）；
        // 刪除職稱時由 JobTitleHandler.DeleteAsync 統一清洗。
        builder.HasOne(x => x.JobTitle)
               .WithMany()
               .HasForeignKey(x => x.JobTitleId)
               .OnDelete(DeleteBehavior.NoAction);

        // 同一步驟同一職稱只能有一列
        builder.HasIndex(x => new { x.ApprovalStepId, x.JobTitleId }).IsUnique();
    }
}
