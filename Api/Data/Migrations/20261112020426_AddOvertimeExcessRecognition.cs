using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOvertimeExcessRecognition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExtraRecognition",
                table: "OvertimeRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecognitionHandledAt",
                table: "OvertimeRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecognitionHandledById",
                table: "OvertimeRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecognitionHandledByName",
                table: "OvertimeRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecognitionReason",
                table: "OvertimeRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecognitionSupervisor",
                table: "OvertimeRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecognizedHours",
                table: "OvertimeRequests",
                type: "decimal(5,1)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "AttendanceAuditLogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtraRecognition",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "RecognitionHandledAt",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "RecognitionHandledById",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "RecognitionHandledByName",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "RecognitionReason",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "RecognitionSupervisor",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "RecognizedHours",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "AttendanceAuditLogs");
        }
    }
}
