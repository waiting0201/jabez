using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class OvertimeRequestProjectConfiguration : IEntityTypeConfiguration<OvertimeRequestProject>
{
    public void Configure(EntityTypeBuilder<OvertimeRequestProject> builder)
    {
        builder.ToTable("OvertimeRequestProjects");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EstimatedHours)
               .HasColumnType("decimal(5,1)");   // 與父表 OvertimeRequest.EstimatedHours 同精度

        builder.HasOne(x => x.OvertimeRequest)
               .WithMany(o => o.Projects)
               .HasForeignKey(x => x.OvertimeRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        // 沿用 ApprovalStepDesignatedJobTitle 慣例：雙 FK 子表的第二個主檔一律 NoAction，
        // 兩邊 Cascade 會撞 SQL Server 1785 multiple cascade paths。
        // 刪除專案時由 ProjectHandler.DeleteAsync 阻擋（清洗會使父表合計快取失真）。
        builder.HasOne(x => x.Project)
               .WithMany()
               .HasForeignKey(x => x.ProjectId)
               .OnDelete(DeleteBehavior.NoAction);

        // 同一張加班單同一專案只能有一列
        builder.HasIndex(x => new { x.OvertimeRequestId, x.ProjectId }).IsUnique();

        // 加班報表 projectId 篩選（EXISTS）與 ProjectHandler 刪除阻擋以 ProjectId 反查
        builder.HasIndex(x => x.ProjectId);

        // 無 Seed data — 由加班申請操作產生
    }
}
