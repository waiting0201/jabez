using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StepOrder)
               .IsRequired();

        builder.Property(s => s.UseApplicantDepartment)
               .HasDefaultValue(false);

        builder.Property(s => s.Note)
               .HasMaxLength(500);

        builder.Property(s => s.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.HasOne(s => s.ApprovalItem)
               .WithMany(a => a.Steps)
               .HasForeignKey(s => s.ApprovalItemId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Department)
               .WithMany()
               .HasForeignKey(s => s.DepartmentId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.JobTitle)
               .WithMany()
               .HasForeignKey(s => s.JobTitleId)
               .OnDelete(DeleteBehavior.SetNull);

        // Seed: 請款申請四步驟，加班/請假/出差各一步驟
        builder.HasData(
            // 請款申請: Step1=申請人部門主管初核 → Step2=會計部主管審核 → Step3=財務部主管撥款 → Step4=董事長最終核決
            new ApprovalStep { Id = 3, ApprovalItemId = 2, StepOrder = 1, DepartmentId = null, JobTitleId = 4, UseApplicantDepartment = true,  Note = "部門主管初核",                             CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 4, ApprovalItemId = 2, StepOrder = 2, DepartmentId = 1,    JobTitleId = 4, UseApplicantDepartment = false, Note = "取得紙本資料審核",                         CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 8, ApprovalItemId = 2, StepOrder = 3, DepartmentId = 2,    JobTitleId = 4, UseApplicantDepartment = false, Note = "填入預計撥款日，核決及撥款後，填入撥款日", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 9, ApprovalItemId = 2, StepOrder = 4, DepartmentId = 4,    JobTitleId = 5, UseApplicantDepartment = false, Note = "最終核決",                                 CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            // 加班申請: Step1=申請人部門主管
            new ApprovalStep { Id = 5, ApprovalItemId = 3, StepOrder = 1, DepartmentId = null, JobTitleId = 4, UseApplicantDepartment = true,  CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            // 請假申請: Step1=申請人部門主管
            new ApprovalStep { Id = 6, ApprovalItemId = 4, StepOrder = 1, DepartmentId = null, JobTitleId = 4, UseApplicantDepartment = true,  CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            // 出差申請: Step1=申請人部門主管
            new ApprovalStep { Id = 7, ApprovalItemId = 5, StepOrder = 1, DepartmentId = null, JobTitleId = 4, UseApplicantDepartment = true,  CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
