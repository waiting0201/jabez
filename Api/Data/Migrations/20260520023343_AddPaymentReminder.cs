using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentReminderDaysBefore",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PaymentReminderLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TickedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TickedAtTaipei = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReminderDateTaipei = table.Column<DateOnly>(type: "date", nullable: false),
                    TriggerSource = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TriggeredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinanceUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LineUserIdSnapshot = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserNameSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ErrorCategory = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReminderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentReminderLogs_Users_FinanceUserId",
                        column: x => x.FinanceUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentReminderLogs_Users_TriggeredByUserId",
                        column: x => x.TriggeredByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "PaymentReminderDaysBefore",
                value: 3);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReminderLogs_BatchId",
                table: "PaymentReminderLogs",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReminderLogs_Date_Status",
                table: "PaymentReminderLogs",
                columns: new[] { "ReminderDateTaipei", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReminderLogs_FinanceUser_Date",
                table: "PaymentReminderLogs",
                columns: new[] { "FinanceUserId", "ReminderDateTaipei" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReminderLogs_TriggeredByUserId",
                table: "PaymentReminderLogs",
                column: "TriggeredByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentReminderLogs");

            migrationBuilder.DropColumn(
                name: "PaymentReminderDaysBefore",
                table: "SystemSettings");
        }
    }
}
