using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentToApprovalItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApprovalItems_ApplicationType",
                table: "ApprovalItems");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "ApprovalItems",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 2,
                column: "DepartmentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 3,
                column: "DepartmentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 4,
                column: "DepartmentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 5,
                column: "DepartmentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 6,
                column: "DepartmentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 7,
                column: "DepartmentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 8,
                column: "DepartmentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 9,
                column: "DepartmentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 10,
                column: "DepartmentId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalItems_ApplicationType_DepartmentId",
                table: "ApprovalItems",
                columns: new[] { "ApplicationType", "DepartmentId" },
                unique: true,
                filter: "[ApplicationType] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalItems_DepartmentId",
                table: "ApprovalItems",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalItems_Departments_DepartmentId",
                table: "ApprovalItems",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalItems_Departments_DepartmentId",
                table: "ApprovalItems");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalItems_ApplicationType_DepartmentId",
                table: "ApprovalItems");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalItems_DepartmentId",
                table: "ApprovalItems");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "ApprovalItems");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalItems_ApplicationType",
                table: "ApprovalItems",
                column: "ApplicationType",
                unique: true,
                filter: "[ApplicationType] IS NOT NULL");
        }
    }
}
