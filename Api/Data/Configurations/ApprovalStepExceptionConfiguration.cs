using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ApprovalStepExceptionConfiguration : IEntityTypeConfiguration<ApprovalStepException>
{
    public void Configure(EntityTypeBuilder<ApprovalStepException> builder)
    {
        builder.ToTable("ApprovalStepExceptions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.HasOne(x => x.ApprovalStep)
               .WithMany(s => s.Exceptions)
               .HasForeignKey(x => x.ApprovalStepId)
               .OnDelete(DeleteBehavior.Cascade);

        // 沿用 RequestDesignatedReviewer 慣例：指向 Users 的 FK 一律 NoAction，
        // 刪除使用者時由 UserHandler.DeleteAsync 統一清洗。
        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.NoAction);

        // 同一步驟同一人只能有一列
        builder.HasIndex(x => new { x.ApprovalStepId, x.UserId }).IsUnique();

        // GET /approval-items/active 以 UserId 反查是否命中例外
        builder.HasIndex(x => x.UserId);
    }
}
