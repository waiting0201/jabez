using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <summary>
    /// ① 下班打卡的專案工時佔比表 AttendanceProjectAllocations。
    /// ② 新增權限碼 reports-work-hours:read（工時總表），**刻意不回填任何角色**，由 Superadmin 到角色管理指派後請該員重新登入。
    ///
    /// 權限以 raw SQL 冪等落地（取代 scaffold 的 InsertData；PermissionConfiguration.HasData 的 Id 79 只為 snapshot 對得上）：
    /// Program.cs 啟動時會自動 MigrateAsync，Id 79 若已被 UI 建立的權限占用（PermissionHandler 以 max+1 配號）
    /// 直接 InsertData 會撞 PK、整個 Function App 起不來。故 Id 優先取 79，被占用則取 max(數字 Id)+1，之後一律以 Code 解析。
    /// Module 沿用同 DB 內 reports-overtime:read 那筆（正式站可能已改成 N'統計報表'，與 HasData 的 'Reports' 不同）。
    /// </summary>
    public partial class AddAttendanceProjectAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceProjectAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendanceRecordId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceProjectAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceProjectAllocations_AttendanceRecords_AttendanceRecordId",
                        column: x => x.AttendanceRecordId,
                        principalTable: "AttendanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceProjectAllocations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Code = 'reports-work-hours:read')
                BEGIN
                    DECLARE @NewId nvarchar(450) = '79';
                    IF EXISTS (SELECT 1 FROM Permissions WHERE Id = @NewId)
                        SELECT @NewId = CAST(ISNULL(MAX(TRY_CAST(Id AS int)), 0) + 1 AS nvarchar(450)) FROM Permissions;

                    INSERT INTO Permissions (Id, Code, Name, Module, Description)
                    SELECT @NewId, 'reports-work-hours:read', N'工時總表',
                           ISNULL((SELECT TOP 1 Module FROM Permissions WHERE Code = 'reports-overtime:read'), N'Reports'),
                           NULL;
                END
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceProjectAllocations_AttendanceRecordId_ProjectId",
                table: "AttendanceProjectAllocations",
                columns: new[] { "AttendanceRecordId", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceProjectAllocations_ProjectId",
                table: "AttendanceProjectAllocations",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceProjectAllocations");

            migrationBuilder.Sql("""
                DELETE FROM RolePermissions
                WHERE PermissionId IN (SELECT Id FROM Permissions WHERE Code = 'reports-work-hours:read');

                DELETE FROM Permissions WHERE Code = 'reports-work-hours:read';
                """);
        }
    }
}
