using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHrProfileAndDependents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisabledProofUrl",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HealthInsuranceOverride",
                table: "Users",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDisabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLowIncome",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LaborInsuranceOverride",
                table: "Users",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LowIncomeProofUrl",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EnglishName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IdNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MaritalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BirthPlace = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MobilePhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResidentialAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResidentialPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MailingAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MailingPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BankCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BankAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InsuranceStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DependentCount = table.Column<int>(type: "int", nullable: true),
                    Specialties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResignationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdCardFrontUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdCardBackUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_EmployeeProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EducationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    School = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Degree = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    EmployeeProfileUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationRecords_EmployeeProfiles_EmployeeProfileUserId",
                        column: x => x.EmployeeProfileUserId,
                        principalTable: "EmployeeProfiles",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_EducationRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentHistoryRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Organization = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    EmployeeProfileUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentHistoryRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmploymentHistoryRecords_EmployeeProfiles_EmployeeProfileUserId",
                        column: x => x.EmployeeProfileUserId,
                        principalTable: "EmployeeProfiles",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_EmploymentHistoryRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FamilyMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Age = table.Column<int>(type: "int", nullable: true),
                    Occupation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    EmployeeProfileUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyMembers_EmployeeProfiles_EmployeeProfileUserId",
                        column: x => x.EmployeeProfileUserId,
                        principalTable: "EmployeeProfiles",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_FamilyMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthInsuranceDependents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IdNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    EmployeeProfileUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthInsuranceDependents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthInsuranceDependents_EmployeeProfiles_EmployeeProfileUserId",
                        column: x => x.EmployeeProfileUserId,
                        principalTable: "EmployeeProfiles",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_HealthInsuranceDependents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobTransferRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FromDepartment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ToDepartment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FromJobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ToJobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    EmployeeProfileUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTransferRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobTransferRecords_EmployeeProfiles_EmployeeProfileUserId",
                        column: x => x.EmployeeProfileUserId,
                        principalTable: "EmployeeProfiles",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_JobTransferRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LanguageAbilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Listening = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Speaking = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Reading = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Writing = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    EmployeeProfileUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanguageAbilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LanguageAbilities_EmployeeProfiles_EmployeeProfileUserId",
                        column: x => x.EmployeeProfileUserId,
                        principalTable: "EmployeeProfiles",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_LanguageAbilities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfessionalTrainings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainingName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TrainingOrg = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Hours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    EmployeeProfileUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalTrainings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfessionalTrainings_EmployeeProfiles_EmployeeProfileUserId",
                        column: x => x.EmployeeProfileUserId,
                        principalTable: "EmployeeProfiles",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_ProfessionalTrainings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RewardPunishmentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Count = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    EmployeeProfileUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardPunishmentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RewardPunishmentRecords_EmployeeProfiles_EmployeeProfileUserId",
                        column: x => x.EmployeeProfileUserId,
                        principalTable: "EmployeeProfiles",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_RewardPunishmentRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalaryAdjustmentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PositionAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DutyAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OtherAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AdjustmentDifference = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OverseasAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MealAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())"),
                    EmployeeProfileUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryAdjustmentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryAdjustmentRecords_EmployeeProfiles_EmployeeProfileUserId",
                        column: x => x.EmployeeProfileUserId,
                        principalTable: "EmployeeProfiles",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_SalaryAdjustmentRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DisabledProofUrl", "HealthInsuranceOverride", "LaborInsuranceOverride", "LowIncomeProofUrl" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "DisabledProofUrl", "HealthInsuranceOverride", "LaborInsuranceOverride", "LowIncomeProofUrl" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "DisabledProofUrl", "HealthInsuranceOverride", "LaborInsuranceOverride", "LowIncomeProofUrl" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("281c2016-801e-48eb-b73b-751643464f48"),
                columns: new[] { "DisabledProofUrl", "HealthInsuranceOverride", "LaborInsuranceOverride", "LowIncomeProofUrl" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "DisabledProofUrl", "HealthInsuranceOverride", "LaborInsuranceOverride", "LowIncomeProofUrl" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("6452ad1e-9648-4194-8fb0-0ac55a76f992"),
                columns: new[] { "DisabledProofUrl", "HealthInsuranceOverride", "LaborInsuranceOverride", "LowIncomeProofUrl" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("6a4002be-23e0-4343-8092-f221b97c5098"),
                columns: new[] { "DisabledProofUrl", "HealthInsuranceOverride", "LaborInsuranceOverride", "LowIncomeProofUrl" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("83f6b1f7-2f25-4f9b-b102-37d1a27f0b35"),
                columns: new[] { "DisabledProofUrl", "HealthInsuranceOverride", "LaborInsuranceOverride", "LowIncomeProofUrl" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b56b8afd-1663-4317-9007-4560da27239d"),
                columns: new[] { "DisabledProofUrl", "HealthInsuranceOverride", "LaborInsuranceOverride", "LowIncomeProofUrl" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("df5d56ad-dd46-4fca-948c-d8301610997a"),
                columns: new[] { "DisabledProofUrl", "HealthInsuranceOverride", "LaborInsuranceOverride", "LowIncomeProofUrl" },
                values: new object[] { null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_EducationRecords_EmployeeProfileUserId",
                table: "EducationRecords",
                column: "EmployeeProfileUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EducationRecords_UserId_Order",
                table: "EducationRecords",
                columns: new[] { "UserId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentHistoryRecords_EmployeeProfileUserId",
                table: "EmploymentHistoryRecords",
                column: "EmployeeProfileUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentHistoryRecords_UserId_Order",
                table: "EmploymentHistoryRecords",
                columns: new[] { "UserId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMembers_EmployeeProfileUserId",
                table: "FamilyMembers",
                column: "EmployeeProfileUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMembers_UserId",
                table: "FamilyMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthInsuranceDependents_EmployeeProfileUserId",
                table: "HealthInsuranceDependents",
                column: "EmployeeProfileUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthInsuranceDependents_UserId",
                table: "HealthInsuranceDependents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTransferRecords_EmployeeProfileUserId",
                table: "JobTransferRecords",
                column: "EmployeeProfileUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTransferRecords_UserId",
                table: "JobTransferRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LanguageAbilities_EmployeeProfileUserId",
                table: "LanguageAbilities",
                column: "EmployeeProfileUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LanguageAbilities_UserId",
                table: "LanguageAbilities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalTrainings_EmployeeProfileUserId",
                table: "ProfessionalTrainings",
                column: "EmployeeProfileUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalTrainings_UserId",
                table: "ProfessionalTrainings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardPunishmentRecords_EmployeeProfileUserId",
                table: "RewardPunishmentRecords",
                column: "EmployeeProfileUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardPunishmentRecords_UserId",
                table: "RewardPunishmentRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryAdjustmentRecords_EmployeeProfileUserId",
                table: "SalaryAdjustmentRecords",
                column: "EmployeeProfileUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryAdjustmentRecords_UserId_EffectiveDate",
                table: "SalaryAdjustmentRecords",
                columns: new[] { "UserId", "EffectiveDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EducationRecords");

            migrationBuilder.DropTable(
                name: "EmploymentHistoryRecords");

            migrationBuilder.DropTable(
                name: "FamilyMembers");

            migrationBuilder.DropTable(
                name: "HealthInsuranceDependents");

            migrationBuilder.DropTable(
                name: "JobTransferRecords");

            migrationBuilder.DropTable(
                name: "LanguageAbilities");

            migrationBuilder.DropTable(
                name: "ProfessionalTrainings");

            migrationBuilder.DropTable(
                name: "RewardPunishmentRecords");

            migrationBuilder.DropTable(
                name: "SalaryAdjustmentRecords");

            migrationBuilder.DropTable(
                name: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "DisabledProofUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HealthInsuranceOverride",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDisabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsLowIncome",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LaborInsuranceOverride",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LowIncomeProofUrl",
                table: "Users");
        }
    }
}
