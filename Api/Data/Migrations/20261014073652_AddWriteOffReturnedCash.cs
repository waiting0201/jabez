using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWriteOffReturnedCash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedAmount",
                table: "WriteOffRecords",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnedAt",
                table: "WriteOffRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnedProofFileName",
                table: "WriteOffRecords",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnedProofUrl",
                table: "WriteOffRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnedAmount",
                table: "WriteOffRecords");

            migrationBuilder.DropColumn(
                name: "ReturnedAt",
                table: "WriteOffRecords");

            migrationBuilder.DropColumn(
                name: "ReturnedProofFileName",
                table: "WriteOffRecords");

            migrationBuilder.DropColumn(
                name: "ReturnedProofUrl",
                table: "WriteOffRecords");
        }
    }
}
