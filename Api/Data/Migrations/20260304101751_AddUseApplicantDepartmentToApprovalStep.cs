using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUseApplicantDepartmentToApprovalStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseApplicantDepartment",
                table: "ApprovalSteps",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DepartmentId", "UseApplicantDepartment" },
                values: new object[] { null, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseApplicantDepartment",
                table: "ApprovalSteps");

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 1,
                column: "DepartmentId",
                value: 1);
        }
    }
}
