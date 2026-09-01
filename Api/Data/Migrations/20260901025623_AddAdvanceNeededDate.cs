using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvanceNeededDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdvanceNeededDate",
                table: "TravelRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AdvanceNeededDate",
                table: "AdvanceRequestSupplements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AdvanceNeededDate",
                table: "AdvanceRequests",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvanceNeededDate",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "AdvanceNeededDate",
                table: "AdvanceRequestSupplements");

            migrationBuilder.DropColumn(
                name: "AdvanceNeededDate",
                table: "AdvanceRequests");
        }
    }
}
