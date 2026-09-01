using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSecondBankAccountToEmployeeProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccount2",
                table: "EmployeeProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankBookImageUrl2",
                table: "EmployeeProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankCode2",
                table: "EmployeeProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccount2",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "BankBookImageUrl2",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "BankCode2",
                table: "EmployeeProfiles");
        }
    }
}
