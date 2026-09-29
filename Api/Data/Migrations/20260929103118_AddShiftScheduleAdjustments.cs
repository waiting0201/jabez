using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftScheduleAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShiftScheduleAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityDayId = table.Column<int>(type: "int", nullable: true),
                    ActivityTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    OriginalDayType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RelocatedTo = table.Column<DateTime>(type: "date", nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftScheduleAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftScheduleAdjustments_ActivityDays_ActivityDayId",
                        column: x => x.ActivityDayId,
                        principalTable: "ActivityDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShiftScheduleAdjustments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftScheduleAdjustments_ActivityDayId",
                table: "ShiftScheduleAdjustments",
                column: "ActivityDayId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftScheduleAdjustments_UserId_AcknowledgedAt",
                table: "ShiftScheduleAdjustments",
                columns: new[] { "UserId", "AcknowledgedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShiftScheduleAdjustments");
        }
    }
}
