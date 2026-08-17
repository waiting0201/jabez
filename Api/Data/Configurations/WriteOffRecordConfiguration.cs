using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class WriteOffRecordConfiguration : IEntityTypeConfiguration<WriteOffRecord>
{
    public void Configure(EntityTypeBuilder<WriteOffRecord> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.RequestNo)
               .IsRequired()
               .HasMaxLength(30)
               .HasDefaultValue("");

        // 單號唯一（比照其他 5 種申請單）：CreateAsync 以 MAX(RequestNo)+1 取號，
        // 併發（例如使用者連按送出）時兩筆會取到同號，靠此索引擋下第二筆。
        // 註：資料庫早在 20260320072900 就以手寫 SQL 建了此索引，但 EF 設定漏宣告，model snapshot 一直沒有它。
        builder.HasIndex(w => w.RequestNo)
               .IsUnique();

        builder.Property(w => w.CashTotal)
               .HasColumnType("decimal(18,2)");

        builder.Property(w => w.CheckTotal)
               .HasColumnType("decimal(18,2)");

        builder.Property(w => w.GrandTotal)
               .HasColumnType("decimal(18,2)");

        builder.Property(w => w.Note)
               .HasMaxLength(1000);

        // 簽核流程欄位
        builder.Property(w => w.ApprovalStatus)
               .IsRequired()
               .HasMaxLength(20)
               .HasDefaultValue("draft");

        builder.Property(w => w.CurrentStepOrder)
               .HasDefaultValue(1);

        builder.Property(w => w.ReviewNote)
               .HasMaxLength(1000);

        // 待結案登記：財務勾選當下只登記，整張單核准才寫 AdvanceRequest.IsClosed
        builder.Property(w => w.PendingClose)
               .HasDefaultValue(false);

        builder.HasOne(w => w.AdvanceRequest)
               .WithMany(a => a.WriteOffs)
               .HasForeignKey(w => w.AdvanceRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.SubmittedBy)
               .WithMany()
               .HasForeignKey(w => w.SubmittedById)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(w => w.ApprovalItem)
               .WithMany()
               .HasForeignKey(w => w.ApprovalItemId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(w => w.ReviewedBy)
               .WithMany()
               .HasForeignKey(w => w.ReviewedById)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
