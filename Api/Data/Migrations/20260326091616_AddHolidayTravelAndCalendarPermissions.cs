using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHolidayTravelAndCalendarPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HolidayDays",
                table: "TravelRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "TravelRequestItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "TravelRequestItems",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoiceDate",
                table: "TravelRequestItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNo",
                table: "TravelRequestItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CalendarDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsHoliday = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarDays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TravelRequestParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TravelRequestId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelRequestParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelRequestParticipants_TravelRequests_TravelRequestId",
                        column: x => x.TravelRequestId,
                        principalTable: "TravelRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TravelRequestParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ApprovalItems",
                columns: new[] { "Id", "ApplicationType", "Code", "CreatedAt", "Description", "IsActive", "Name" },
                values: new object[] { 9, "holiday_travel", "holiday_travel_request", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "假日出差申請" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { "60", "holiday-travel-requests:read", null, "假日出差申請", "瀏覽" },
                    { "61", "holiday-travel-requests:write", null, "假日出差申請", "新增/修改" },
                    { "62", "holiday-travel-requests:delete", null, "假日出差申請", "刪除" },
                    { "63", "calendar-days:read", null, "行事曆管理", "瀏覽" },
                    { "64", "calendar-days:write", null, "行事曆管理", "新增/修改" },
                    { "65", "calendar-days:delete", null, "行事曆管理", "刪除" }
                });

            migrationBuilder.InsertData(
                table: "ApprovalSteps",
                columns: new[] { "Id", "ApprovalItemId", "CreatedAt", "DepartmentId", "JobTitleId", "Note", "StepOrder", "UseApplicantDesignated" },
                values: new object[] { 40, 9, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "指定初核", 1, true });

            migrationBuilder.InsertData(
                table: "ApprovalSteps",
                columns: new[] { "Id", "ApprovalItemId", "CreatedAt", "DepartmentId", "JobTitleId", "Note", "StepOrder" },
                values: new object[,]
                {
                    { 41, 9, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, 5, "總監核決", 2 },
                    { 42, 9, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 11, "取得紙本資料審核", 3 },
                    { 43, 9, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 7, "填入預計撥款日，核決及撥款後，填入撥款日", 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarDays_Date",
                table: "CalendarDays",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarDays_Year_IsHoliday",
                table: "CalendarDays",
                columns: new[] { "Year", "IsHoliday" });

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequestParticipants_TravelRequestId_UserId",
                table: "TravelRequestParticipants",
                columns: new[] { "TravelRequestId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequestParticipants_UserId",
                table: "TravelRequestParticipants",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarDays");

            migrationBuilder.DropTable(
                name: "TravelRequestParticipants");

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "60");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "61");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "62");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "63");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "64");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "65");

            migrationBuilder.DeleteData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DropColumn(
                name: "HolidayDays",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "TravelRequestItems");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "TravelRequestItems");

            migrationBuilder.DropColumn(
                name: "InvoiceDate",
                table: "TravelRequestItems");

            migrationBuilder.DropColumn(
                name: "InvoiceNo",
                table: "TravelRequestItems");
        }
    }
}
