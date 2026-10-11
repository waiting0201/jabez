using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SecurityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SessionStartedAt",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())");

            migrationBuilder.AddColumn<decimal>(
                name: "SettledHours",
                table: "OvertimeRequests",
                type: "decimal(5,1)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsManuallyAdjusted",
                table: "AttendanceRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAdjustedAt",
                table: "AttendanceRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastAdjustedById",
                table: "AttendanceRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceAuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendanceRecordId = table.Column<int>(type: "int", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "date", nullable: false),
                    ModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClockInBefore = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClockInAfter = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClockOutBefore = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClockOutAfter = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OvertimeStartBefore = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OvertimeStartAfter = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OvertimeEndBefore = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OvertimeEndAfter = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemarkBefore = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RemarkAfter = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceAuditLogs_AttendanceRecords_AttendanceRecordId",
                        column: x => x.AttendanceRecordId,
                        principalTable: "AttendanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAuditLogs_AttendanceRecordId_ModifiedAt",
                table: "AttendanceAuditLogs",
                columns: new[] { "AttendanceRecordId", "ModifiedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAuditLogs_OwnerUserId_RecordDate",
                table: "AttendanceAuditLogs",
                columns: new[] { "OwnerUserId", "RecordDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Email_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "Email", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_UserId_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "UserId", "AttemptedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceAuditLogs");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "SessionStartedAt",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "SettledHours",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "IsManuallyAdjusted",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "LastAdjustedAt",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "LastAdjustedById",
                table: "AttendanceRecords");
        }
    }
}
