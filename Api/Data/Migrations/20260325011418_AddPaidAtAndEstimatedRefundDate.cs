using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidAtAndEstimatedRefundDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedRefundDate",
                table: "TravelRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "TravelRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedRefundDate",
                table: "AdvanceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "AdvanceRequests",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedRefundDate",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedRefundDate",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "AdvanceRequests");
        }
    }
}
