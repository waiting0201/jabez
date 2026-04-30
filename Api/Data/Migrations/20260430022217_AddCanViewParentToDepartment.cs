using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCanViewParentToDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanViewParent",
                table: "Departments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanViewParent",
                table: "Departments");
        }
    }
}
