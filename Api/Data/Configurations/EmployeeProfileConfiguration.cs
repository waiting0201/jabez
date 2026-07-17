using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class EmployeeProfileConfiguration : IEntityTypeConfiguration<EmployeeProfile>
{
    public void Configure(EntityTypeBuilder<EmployeeProfile> builder)
    {
        // PK = UserId（1:1 with User）
        builder.HasKey(p => p.UserId);

        builder.Property(p => p.EmployeeNumber)
               .HasMaxLength(50);

        builder.Property(p => p.EnglishName)
               .HasMaxLength(200);

        builder.Property(p => p.IdNumber)
               .HasMaxLength(20);

        builder.Property(p => p.Gender)
               .HasMaxLength(10);

        builder.Property(p => p.MaritalStatus)
               .HasMaxLength(20);

        builder.Property(p => p.BirthPlace)
               .HasMaxLength(200);

        builder.Property(p => p.MobilePhone)
               .HasMaxLength(50);

        builder.Property(p => p.ResidentialAddress)
               .HasMaxLength(500);

        builder.Property(p => p.ResidentialPhone)
               .HasMaxLength(50);

        builder.Property(p => p.MailingAddress)
               .HasMaxLength(500);

        builder.Property(p => p.MailingPhone)
               .HasMaxLength(50);

        builder.Property(p => p.EmergencyContactName)
               .HasMaxLength(100);

        builder.Property(p => p.EmergencyContactPhone)
               .HasMaxLength(50);

        builder.Property(p => p.BankCode)
               .HasMaxLength(20);

        builder.Property(p => p.BankAccount)
               .HasMaxLength(50);

        builder.Property(p => p.Specialties)
               .HasColumnType("nvarchar(max)");

        builder.Property(p => p.ResignationReason)
               .HasColumnType("nvarchar(max)");

        builder.Property(p => p.IdCardFrontUrl)
               .HasMaxLength(500);

        builder.Property(p => p.IdCardBackUrl)
               .HasMaxLength(500);

        builder.Property(p => p.HighestEducationProofUrl)
               .HasMaxLength(500);

        builder.Property(p => p.BankBookImageUrl)
               .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.Property(p => p.UpdatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        // 1:1 FK → User（Cascade：刪除 User 時同時刪除 EmployeeProfile）
        builder.HasOne<User>()
               .WithOne()
               .HasForeignKey<EmployeeProfile>(p => p.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        // 無 Seed data — 人事資料卡由管理員手動填寫
    }
}
