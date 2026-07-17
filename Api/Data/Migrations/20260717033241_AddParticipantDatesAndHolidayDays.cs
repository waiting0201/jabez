using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantDatesAndHolidayDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HolidayDays",
                table: "TravelRequestParticipants",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TravelRequestParticipantDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TravelRequestParticipantId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelRequestParticipantDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelRequestParticipantDates_TravelRequestParticipants_TravelRequestParticipantId",
                        column: x => x.TravelRequestParticipantId,
                        principalTable: "TravelRequestParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequestParticipantDates_TravelRequestParticipantId_Date",
                table: "TravelRequestParticipantDates",
                columns: new[] { "TravelRequestParticipantId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TravelRequestParticipantDates");

            migrationBuilder.DropColumn(
                name: "HolidayDays",
                table: "TravelRequestParticipants");
        }
    }
}
