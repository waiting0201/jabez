using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentVisibilityAndProjectRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 既有 NULL 專案先回填為 總監室 (Id=4)，避免後續 NOT NULL ALTER 失敗
            migrationBuilder.Sql("UPDATE Projects SET DepartmentId = 4 WHERE DepartmentId IS NULL;");

            // 2. 暫時移除 FK（才能調整欄位 nullable 與 delete behavior）
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Departments_DepartmentId",
                table: "Projects");

            // 3. Projects.DepartmentId → NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // 4. Departments.CanViewSiblings（同層兄弟部門專案是否可見；預設 false）
            migrationBuilder.AddColumn<bool>(
                name: "CanViewSiblings",
                table: "Departments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // 5. 重建 FK：DeleteBehavior.Restrict（避免刪除部門時誤刪專案）
            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Departments_DepartmentId",
                table: "Projects",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Departments_DepartmentId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CanViewSiblings",
                table: "Departments");

            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "Projects",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Departments_DepartmentId",
                table: "Projects",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
