using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFlexibleWorkScheduleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CredentialsSentAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FlexibleWorkStartDate",
                table: "SystemSettings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActivityDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityDays_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CompensatoryLots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceOvertimeRequestId = table.Column<int>(type: "int", nullable: true),
                    EarnedDate = table.Column<DateTime>(type: "date", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(6,1)", nullable: false),
                    RemainingHours = table.Column<decimal>(type: "decimal(6,1)", nullable: false),
                    RateSnapshot = table.Column<decimal>(type: "decimal(4,2)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsOpening = table.Column<bool>(type: "bit", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SettledAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompensatoryLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompensatoryLots_OvertimeRequests_SourceOvertimeRequestId",
                        column: x => x.SourceOvertimeRequestId,
                        principalTable: "OvertimeRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompensatoryLots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShiftScheduleDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    DayType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftScheduleDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftScheduleDays_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShiftScheduleMonths",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    CommittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AutoAssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftScheduleMonths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftScheduleMonths_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ActivityDayAssignees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityDayId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityDayAssignees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityDayAssignees_ActivityDays_ActivityDayId",
                        column: x => x.ActivityDayId,
                        principalTable: "ActivityDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityDayAssignees_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CompensatoryUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LotId = table.Column<int>(type: "int", nullable: false),
                    LeaveRequestId = table.Column<int>(type: "int", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(6,1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompensatoryUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompensatoryUsages_CompensatoryLots_LotId",
                        column: x => x.LotId,
                        principalTable: "CompensatoryLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompensatoryUsages_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "FlexibleWorkStartDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "CredentialsSentAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CredentialsSentAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CredentialsSentAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("281c2016-801e-48eb-b73b-751643464f48"),
                column: "CredentialsSentAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CredentialsSentAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("6452ad1e-9648-4194-8fb0-0ac55a76f992"),
                column: "CredentialsSentAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("6a4002be-23e0-4343-8092-f221b97c5098"),
                column: "CredentialsSentAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("83f6b1f7-2f25-4f9b-b102-37d1a27f0b35"),
                column: "CredentialsSentAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b56b8afd-1663-4317-9007-4560da27239d"),
                column: "CredentialsSentAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("df5d56ad-dd46-4fca-948c-d8301610997a"),
                column: "CredentialsSentAt",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDayAssignees_ActivityDayId_UserId",
                table: "ActivityDayAssignees",
                columns: new[] { "ActivityDayId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDayAssignees_UserId",
                table: "ActivityDayAssignees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDays_Date",
                table: "ActivityDays",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDays_DepartmentId_Date",
                table: "ActivityDays",
                columns: new[] { "DepartmentId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_CompensatoryLots_ExpiresAt_SettledAt",
                table: "CompensatoryLots",
                columns: new[] { "ExpiresAt", "SettledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CompensatoryLots_SourceOvertimeRequestId",
                table: "CompensatoryLots",
                column: "SourceOvertimeRequestId",
                unique: true,
                filter: "[SourceOvertimeRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompensatoryLots_UserId_EarnedDate",
                table: "CompensatoryLots",
                columns: new[] { "UserId", "EarnedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CompensatoryUsages_LeaveRequestId",
                table: "CompensatoryUsages",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CompensatoryUsages_LotId",
                table: "CompensatoryUsages",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftScheduleDays_Date",
                table: "ShiftScheduleDays",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftScheduleDays_UserId_Date",
                table: "ShiftScheduleDays",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftScheduleMonths_UserId_Year_Month",
                table: "ShiftScheduleMonths",
                columns: new[] { "UserId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftScheduleMonths_Year_Month",
                table: "ShiftScheduleMonths",
                columns: new[] { "Year", "Month" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityDayAssignees");

            migrationBuilder.DropTable(
                name: "CompensatoryUsages");

            migrationBuilder.DropTable(
                name: "ShiftScheduleDays");

            migrationBuilder.DropTable(
                name: "ShiftScheduleMonths");

            migrationBuilder.DropTable(
                name: "ActivityDays");

            migrationBuilder.DropTable(
                name: "CompensatoryLots");

            migrationBuilder.DropColumn(
                name: "CredentialsSentAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FlexibleWorkStartDate",
                table: "SystemSettings");
        }
    }
}
