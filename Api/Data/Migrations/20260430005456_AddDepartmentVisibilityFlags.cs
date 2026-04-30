using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentVisibilityFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanSeeAll",
                table: "Departments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewDescendants",
                table: "Departments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Seed：原本由 ProjectAccessResolver Rule 2 寫死判定的「財務體系部門」（AC / FIN / Jabez HQ / CEO）
            // 改為由資料庫旗標 CanSeeAll 控制。為避免上線後既有部門失去 SeeAll 權限，於此 Migration 將其設為 true。
            // 之後可由部門 CRUD 頁動態調整。
            migrationBuilder.Sql(@"
                UPDATE Departments
                SET CanSeeAll = 1
                WHERE Code IN (N'AC', N'FIN', N'Jabez HQ', N'CEO');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanSeeAll",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "CanViewDescendants",
                table: "Departments");
        }
    }
}
