using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncSeedDataWithCurrentDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 使用 idempotent SQL，確保不管資料庫目前狀態都能安全執行
            migrationBuilder.Sql("""
                -- ── 清除舊 seed data（如果存在）────────────────────────────────
                DELETE FROM ApprovalSteps WHERE Id IN (1, 2);
                DELETE FROM ApprovalItems WHERE Id = 1;

                -- ── Departments（先於 ApprovalSteps，因 Step 9 參照 DepartmentId=4）
                UPDATE Departments SET Code = 'AC',  Name = N'會計部' WHERE Id = 1;
                UPDATE Departments SET Code = 'FIN', Name = N'財務部' WHERE Id = 2;

                IF NOT EXISTS (SELECT 1 FROM Departments WHERE Id = 4)
                BEGIN
                    SET IDENTITY_INSERT Departments ON;
                    INSERT INTO Departments (Id, Code, CreatedAt, Description, Name, ParentId, SortOrder)
                    VALUES (4, 'CO', '2024-01-01', NULL, N'董事長室', NULL, 4);
                    SET IDENTITY_INSERT Departments OFF;
                END

                -- ── JobTitles（先於 ApprovalSteps，因 Step 9 參照 JobTitleId=5）
                IF NOT EXISTS (SELECT 1 FROM JobTitles WHERE Id = 5)
                BEGIN
                    SET IDENTITY_INSERT JobTitles ON;
                    INSERT INTO JobTitles (Id, CreatedAt, Description, Level, Name)
                    VALUES (5, '2024-01-01', NULL, 5, N'董事長');
                    SET IDENTITY_INSERT JobTitles OFF;
                END

                -- ── ApprovalItems ─────────────────────────────────────────────
                UPDATE ApprovalItems SET Code = 'payment_request', Name = N'請款申請' WHERE Id = 2;

                IF NOT EXISTS (SELECT 1 FROM ApprovalItems WHERE Id = 4)
                BEGIN
                    SET IDENTITY_INSERT ApprovalItems ON;
                    INSERT INTO ApprovalItems (Id, ApplicationType, Code, CreatedAt, Description, IsActive, Name)
                    VALUES (4, 'leave', 'leave_request', '2024-01-01', NULL, 1, N'請假申請');
                    SET IDENTITY_INSERT ApprovalItems OFF;
                END

                IF NOT EXISTS (SELECT 1 FROM ApprovalItems WHERE Id = 5)
                BEGIN
                    SET IDENTITY_INSERT ApprovalItems ON;
                    INSERT INTO ApprovalItems (Id, ApplicationType, Code, CreatedAt, Description, IsActive, Name)
                    VALUES (5, 'travel', 'travel_request', '2024-01-01', NULL, 1, N'出差申請');
                    SET IDENTITY_INSERT ApprovalItems OFF;
                END

                -- ── ApprovalSteps ─────────────────────────────────────────────
                UPDATE ApprovalSteps SET UseApplicantDepartment = 1 WHERE Id = 3;
                UPDATE ApprovalSteps SET Note = N'取得紙本資料審核' WHERE Id = 4;

                IF NOT EXISTS (SELECT 1 FROM ApprovalSteps WHERE Id = 5)
                BEGIN
                    SET IDENTITY_INSERT ApprovalSteps ON;
                    INSERT INTO ApprovalSteps (Id, ApprovalItemId, CreatedAt, DepartmentId, JobTitleId, Note, StepOrder, UseApplicantDepartment)
                    VALUES (5, 3, '2024-01-01', NULL, 4, NULL, 1, 1);
                    SET IDENTITY_INSERT ApprovalSteps OFF;
                END

                IF NOT EXISTS (SELECT 1 FROM ApprovalSteps WHERE Id = 6)
                BEGIN
                    SET IDENTITY_INSERT ApprovalSteps ON;
                    INSERT INTO ApprovalSteps (Id, ApprovalItemId, CreatedAt, DepartmentId, JobTitleId, Note, StepOrder, UseApplicantDepartment)
                    VALUES (6, 4, '2024-01-01', NULL, 4, NULL, 1, 1);
                    SET IDENTITY_INSERT ApprovalSteps OFF;
                END

                IF NOT EXISTS (SELECT 1 FROM ApprovalSteps WHERE Id = 7)
                BEGIN
                    SET IDENTITY_INSERT ApprovalSteps ON;
                    INSERT INTO ApprovalSteps (Id, ApprovalItemId, CreatedAt, DepartmentId, JobTitleId, Note, StepOrder, UseApplicantDepartment)
                    VALUES (7, 5, '2024-01-01', NULL, 4, NULL, 1, 1);
                    SET IDENTITY_INSERT ApprovalSteps OFF;
                END

                IF NOT EXISTS (SELECT 1 FROM ApprovalSteps WHERE Id = 8)
                BEGIN
                    SET IDENTITY_INSERT ApprovalSteps ON;
                    INSERT INTO ApprovalSteps (Id, ApprovalItemId, CreatedAt, DepartmentId, JobTitleId, Note, StepOrder, UseApplicantDepartment)
                    VALUES (8, 2, '2024-01-01', 2, 4, N'填入預計撥款日，核決及撥款後，填入撥款日', 3, 0);
                    SET IDENTITY_INSERT ApprovalSteps OFF;
                END

                IF NOT EXISTS (SELECT 1 FROM ApprovalSteps WHERE Id = 9)
                BEGIN
                    SET IDENTITY_INSERT ApprovalSteps ON;
                    INSERT INTO ApprovalSteps (Id, ApprovalItemId, CreatedAt, DepartmentId, JobTitleId, Note, StepOrder, UseApplicantDepartment)
                    VALUES (9, 2, '2024-01-01', 4, 5, N'最終核決', 4, 0);
                    SET IDENTITY_INSERT ApprovalSteps OFF;
                END
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- ── 還原 ApprovalSteps ────────────────────────────────────────
                DELETE FROM ApprovalSteps WHERE Id IN (5, 6, 7, 8, 9);

                UPDATE ApprovalSteps SET UseApplicantDepartment = 0 WHERE Id = 3;
                UPDATE ApprovalSteps SET Note = N'管理部主管最終核可' WHERE Id = 4;

                -- ── 還原 ApprovalItems ────────────────────────────────────────
                DELETE FROM ApprovalItems WHERE Id IN (4, 5);
                UPDATE ApprovalItems SET Code = 'purchase_request', Name = N'採購申請' WHERE Id = 2;

                IF NOT EXISTS (SELECT 1 FROM ApprovalItems WHERE Id = 1)
                BEGIN
                    SET IDENTITY_INSERT ApprovalItems ON;
                    INSERT INTO ApprovalItems (Id, ApplicationType, Code, CreatedAt, Description, IsActive, Name)
                    VALUES (1, 'leave', 'leave_request', '2024-01-01', NULL, 1, N'請假申請');
                    SET IDENTITY_INSERT ApprovalItems OFF;
                END

                IF NOT EXISTS (SELECT 1 FROM ApprovalSteps WHERE Id = 1)
                BEGIN
                    SET IDENTITY_INSERT ApprovalSteps ON;
                    INSERT INTO ApprovalSteps (Id, ApprovalItemId, CreatedAt, DepartmentId, JobTitleId, Note, StepOrder, UseApplicantDepartment)
                    VALUES (1, 1, '2024-01-01', NULL, 4, N'直屬部門主管核可', 1, 1);
                    SET IDENTITY_INSERT ApprovalSteps OFF;
                END

                IF NOT EXISTS (SELECT 1 FROM ApprovalSteps WHERE Id = 2)
                BEGIN
                    SET IDENTITY_INSERT ApprovalSteps ON;
                    INSERT INTO ApprovalSteps (Id, ApprovalItemId, CreatedAt, DepartmentId, JobTitleId, Note, StepOrder, UseApplicantDepartment)
                    VALUES (2, 1, '2024-01-01', 1, NULL, N'管理部最終確認', 2, 0);
                    SET IDENTITY_INSERT ApprovalSteps OFF;
                END

                -- ── 還原 Departments ──────────────────────────────────────────
                DELETE FROM Departments WHERE Id = 4;
                UPDATE Departments SET Code = 'MGT', Name = N'管理部' WHERE Id = 1;
                UPDATE Departments SET Code = 'IT',  Name = N'資訊部' WHERE Id = 2;

                -- ── 還原 JobTitles ────────────────────────────────────────────
                DELETE FROM JobTitles WHERE Id = 5;
            """);
        }
    }
}
