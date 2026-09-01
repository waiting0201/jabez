using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class TravelWriteOffRecordConfiguration : IEntityTypeConfiguration<TravelWriteOffRecord>
{
    public void Configure(EntityTypeBuilder<TravelWriteOffRecord> builder)
    {
        builder.HasKey(w => w.Id);

        // 送簽時才取號（RequestNoGenerator），草稿階段為 null
        builder.Property(w => w.RequestNo)
               .HasMaxLength(30);

        // 單號唯一（比照其他申請單）：SubmitAsync 以 MAX(RequestNo)+1 取號，併發時擋下重複單號
        // filtered index：草稿的 RequestNo 為 NULL，一般唯一索引會視多個 NULL 為衝突，故須排除 NULL
        builder.HasIndex(w => w.RequestNo)
               .IsUnique()
               .HasFilter("[RequestNo] IS NOT NULL");

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

        // 待結案登記：語意同 WriteOffRecord.PendingClose，對應 TravelRequest.IsClosed
        builder.Property(w => w.PendingClose)
               .HasDefaultValue(false);

        builder.HasOne(w => w.TravelRequest)
               .WithMany(t => t.WriteOffs)
               .HasForeignKey(w => w.TravelRequestId)
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
