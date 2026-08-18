using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParentalLeaveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ChildBirthDate",
                table: "LeaveRequests",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ContinueInsurance",
                table: "LeaveRequests",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChildBirthDate",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "ContinueInsurance",
                table: "LeaveRequests");
        }
    }
}
