using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceReminderLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceReminderLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TickedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TickedAtTaipei = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetTimeTaipei = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    ReminderType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TriggerSource = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TriggeredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LineUserIdSnapshot = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserNameSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ErrorCategory = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceReminderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceReminderLogs_Users_TriggeredByUserId",
                        column: x => x.TriggeredByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceReminderLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceReminderLogs_BatchId",
                table: "AttendanceReminderLogs",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceReminderLogs_TickedAtTaipei_Status_Type",
                table: "AttendanceReminderLogs",
                columns: new[] { "TickedAtTaipei", "Status", "ReminderType" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceReminderLogs_TriggeredByUserId",
                table: "AttendanceReminderLogs",
                column: "TriggeredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceReminderLogs_UserId_TickedAtTaipei",
                table: "AttendanceReminderLogs",
                columns: new[] { "UserId", "TickedAtTaipei" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceReminderLogs");
        }
    }
}
