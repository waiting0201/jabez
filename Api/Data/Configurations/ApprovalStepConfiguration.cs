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

        builder.Property(s => s.UseDirectSupervisor)
               .HasDefaultValue(false);

        builder.Property(s => s.UseApplicantDesignated)
               .HasDefaultValue(false);

        builder.Property(s => s.DesignatedRequiresDepartment)
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

        // Seed: 與本機資料庫同步（2026-03-24）
        builder.HasData(
            // 請款申請: Step1=指定初核 → Step2=總監核決 → Step3=會計審核 → Step4=行政財務部CFO撥款
            new ApprovalStep { Id = 3,  ApprovalItemId = 2, StepOrder = 1, DepartmentId = null, JobTitleId = null, UseApplicantDesignated = true, Note = "指定初核",                               CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 9,  ApprovalItemId = 2, StepOrder = 2, DepartmentId = 4,    JobTitleId = 5,    Note = "總監核決",                                                             CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 4,  ApprovalItemId = 2, StepOrder = 3, DepartmentId = 1,    JobTitleId = 11,   Note = "取得紙本資料審核",                                                     CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 8,  ApprovalItemId = 2, StepOrder = 4, DepartmentId = 2,    JobTitleId = 7,    Note = "填入預計撥款日，核決及撥款後，填入撥款日",                               CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            // 加班申請: Step1=指定人審核
            new ApprovalStep { Id = 5,  ApprovalItemId = 3, StepOrder = 1, DepartmentId = null, JobTitleId = null, UseApplicantDesignated = true, Note = "指定人審核", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            // 請假申請: Step1=指定人審核
            new ApprovalStep { Id = 6,  ApprovalItemId = 4, StepOrder = 1, DepartmentId = null, JobTitleId = null, UseApplicantDesignated = true, Note = "指定人審核", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            // 出差申請: Step1=指定人審核
            new ApprovalStep { Id = 7,  ApprovalItemId = 5, StepOrder = 1, DepartmentId = null, JobTitleId = null, UseApplicantDesignated = true, Note = "指定人審核", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            // 預支申請: Step1=指定核准 → Step2=總監簽核 → Step3=會計核對 → Step4=行政財務部CFO撥款
            new ApprovalStep { Id = 10, ApprovalItemId = 6, StepOrder = 1, DepartmentId = null, JobTitleId = null, UseApplicantDesignated = true, Note = "指定核准",                               CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 11, ApprovalItemId = 6, StepOrder = 2, DepartmentId = 4,    JobTitleId = 5,    Note = "總監簽核",                                                             CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 12, ApprovalItemId = 6, StepOrder = 3, DepartmentId = 1,    JobTitleId = 11,   Note = "會計核對",                                                             CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 13, ApprovalItemId = 6, StepOrder = 4, DepartmentId = 2,    JobTitleId = 7,    Note = "填入預計撥款日，撥款後，填入撥款日",                                     CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            // 預支沖銷申請: Step1=指定初核 → Step2=總監核決 → Step3=會計審核 → Step4=行政財務部CFO撥款
            new ApprovalStep { Id = 20, ApprovalItemId = 7, StepOrder = 1, DepartmentId = null, JobTitleId = null, UseApplicantDesignated = true, Note = "指定初核",                               CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 23, ApprovalItemId = 7, StepOrder = 2, DepartmentId = 4,    JobTitleId = 5,    Note = "核決",                                                                 CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 21, ApprovalItemId = 7, StepOrder = 3, DepartmentId = 1,    JobTitleId = 11,   Note = "取得紙本資料審核",                                                     CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 22, ApprovalItemId = 7, StepOrder = 4, DepartmentId = 2,    JobTitleId = 7,    Note = "填入預計撥款日，核決及撥款後，填入撥款日",                               CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            // 出差沖銷申請: Step1=申請人部門專案副理/店長初核 → Step2=總監核決 → Step3=會計審核 → Step4=行政財務部CFO撥款
            new ApprovalStep { Id = 30, ApprovalItemId = 8, StepOrder = 1, DepartmentId = null, JobTitleId = 4,  UseApplicantDepartment = true, Note = "部門主管初核",                             CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 33, ApprovalItemId = 8, StepOrder = 2, DepartmentId = 4,    JobTitleId = 5,  Note = "核決",                                                                   CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 31, ApprovalItemId = 8, StepOrder = 3, DepartmentId = 1,    JobTitleId = 11, Note = "取得紙本資料審核",                                                       CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 32, ApprovalItemId = 8, StepOrder = 4, DepartmentId = 2,    JobTitleId = 7,  Note = "填入預計撥款日，核決及撥款後，填入撥款日",                                 CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            // 假日執行活動申請: Step1=指定初核 → Step2=總監核決 → Step3=會計審核 → Step4=行政財務部CFO撥款
            new ApprovalStep { Id = 40, ApprovalItemId = 9, StepOrder = 1, DepartmentId = null, JobTitleId = null, UseApplicantDesignated = true, Note = "指定初核",                               CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 41, ApprovalItemId = 9, StepOrder = 2, DepartmentId = 4,    JobTitleId = 5,    Note = "總監核決",                                                             CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 42, ApprovalItemId = 9, StepOrder = 3, DepartmentId = 1,    JobTitleId = 11,   Note = "取得紙本資料審核",                                                     CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 43, ApprovalItemId = 9, StepOrder = 4, DepartmentId = 2,    JobTitleId = 7,    Note = "填入預計撥款日，核決及撥款後，填入撥款日",                               CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            // 出差請款申請: Step1=指定初核 → Step2=總監核決 → Step3=會計審核 → Step4=行政財務部CFO撥款
            new ApprovalStep { Id = 50, ApprovalItemId = 10, StepOrder = 1, DepartmentId = null, JobTitleId = null, UseApplicantDesignated = true, Note = "指定初核",                              CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 51, ApprovalItemId = 10, StepOrder = 2, DepartmentId = 4,    JobTitleId = 5,  Note = "總監核決",                                                               CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 52, ApprovalItemId = 10, StepOrder = 3, DepartmentId = 1,    JobTitleId = 11, Note = "取得紙本資料審核",                                                       CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ApprovalStep { Id = 53, ApprovalItemId = 10, StepOrder = 4, DepartmentId = 2,    JobTitleId = 7,  Note = "填入預計撥款日，核決及撥款後，填入撥款日",                                 CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
