using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOvertimeCompensationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompensationType",
                table: "OvertimeRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "compensatory");

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRateSnapshot",
                table: "OvertimeRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHolidayOvertime",
                table: "OvertimeRequests",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimePayAmount",
                table: "OvertimeRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PayableHours",
                table: "OvertimeRequests",
                type: "decimal(5,1)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompensationType",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "HourlyRateSnapshot",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "IsHolidayOvertime",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "OvertimePayAmount",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "PayableHours",
                table: "OvertimeRequests");
        }
    }
}
