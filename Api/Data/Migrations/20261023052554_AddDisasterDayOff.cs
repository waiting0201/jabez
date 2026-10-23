using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDisasterDayOff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkCounty",
                table: "Users",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DisasterDayOffPaidDefault",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DisasterAdjustedHours",
                table: "LeaveRequests",
                type: "decimal(5,1)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "County",
                table: "Departments",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DisasterDayOffFetchLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FetchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    TriggerSource = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ManualItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisasterDayOffFetchLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisasterDayOffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    County = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Segment = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Source = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SourceText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelledByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisasterDayOffs", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "County",
                value: null);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 2,
                column: "County",
                value: null);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 3,
                column: "County",
                value: null);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 4,
                column: "County",
                value: null);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 5,
                column: "County",
                value: null);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 6,
                column: "County",
                value: null);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 7,
                column: "County",
                value: null);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 8,
                column: "County",
                value: null);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 9,
                column: "County",
                value: null);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 10,
                column: "County",
                value: null);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "DisasterDayOffPaidDefault",
                value: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "WorkCounty",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "WorkCounty",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "WorkCounty",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("281c2016-801e-48eb-b73b-751643464f48"),
                column: "WorkCounty",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "WorkCounty",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("6452ad1e-9648-4194-8fb0-0ac55a76f992"),
                column: "WorkCounty",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("6a4002be-23e0-4343-8092-f221b97c5098"),
                column: "WorkCounty",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("83f6b1f7-2f25-4f9b-b102-37d1a27f0b35"),
                column: "WorkCounty",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b56b8afd-1663-4317-9007-4560da27239d"),
                column: "WorkCounty",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("df5d56ad-dd46-4fca-948c-d8301610997a"),
                column: "WorkCounty",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_DisasterDayOffFetchLogs_FetchedAt",
                table: "DisasterDayOffFetchLogs",
                column: "FetchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DisasterDayOffs_Date_Status",
                table: "DisasterDayOffs",
                columns: new[] { "Date", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_DisasterDayOffs_Date_County_Active",
                table: "DisasterDayOffs",
                columns: new[] { "Date", "County" },
                unique: true,
                filter: "[Status] = 'active'");

            // 權限碼 disaster-days:read / :write —— 做法同 AddActivityDayPermissions：Id 不寫死（執行當下取 max+1）、
            // IF NOT EXISTS 包住可重複執行、Module 沿用出勤打卡群組；**刻意不回填任何角色**，
            // 由 Superadmin 到角色管理指派給 HR，指派後請該員重新登入（權限在 JWT claim 內）。
            migrationBuilder.Sql("""
                DECLARE @Module nvarchar(100) =
                    ISNULL((SELECT TOP 1 Module FROM Permissions WHERE Code = 'activity-days:write'),
                           ISNULL((SELECT TOP 1 Module FROM Permissions WHERE Code = 'attendances:read'), N'出勤打卡'));

                DECLARE @NewPerms TABLE (Seq int, Code nvarchar(200), Name nvarchar(200), Module nvarchar(100), Descr nvarchar(1000));
                INSERT INTO @NewPerms (Seq, Code, Name, Module, Descr) VALUES
                    (1, 'disaster-days:read',  N'颱風假瀏覽', @Module, N'檢視天然災害停班（颱風假）清單、影響名單與公告抓取紀錄'),
                    (2, 'disaster-days:write', N'颱風假管理', @Module, N'手動建立 / 修改 / 取消颱風假，並收到公告自動套用的通知');

                DECLARE @Seq int, @Code nvarchar(200), @Name nvarchar(200), @Mod nvarchar(100), @Descr nvarchar(1000);
                DECLARE perm_cursor CURSOR LOCAL FAST_FORWARD FOR
                    SELECT Seq, Code, Name, Module, Descr FROM @NewPerms ORDER BY Seq;
                OPEN perm_cursor;
                FETCH NEXT FROM perm_cursor INTO @Seq, @Code, @Name, @Mod, @Descr;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Code = @Code)
                    BEGIN
                        DECLARE @NextId int =
                            ISNULL((SELECT MAX(TRY_CAST(Id AS int)) FROM Permissions), 0) + 1;

                        INSERT INTO Permissions (Id, Code, Name, Module, Description)
                        VALUES (CAST(@NextId AS nvarchar(20)), @Code, @Name, @Mod, @Descr);
                    END
                    FETCH NEXT FROM perm_cursor INTO @Seq, @Code, @Name, @Mod, @Descr;
                END
                CLOSE perm_cursor;
                DEALLOCATE perm_cursor;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions WHERE Code IN ('disaster-days:read', 'disaster-days:write'));

                DELETE FROM Permissions
                WHERE Code IN ('disaster-days:read', 'disaster-days:write');
                """);

            migrationBuilder.DropTable(
                name: "DisasterDayOffFetchLogs");

            migrationBuilder.DropTable(
                name: "DisasterDayOffs");

            migrationBuilder.DropColumn(
                name: "WorkCounty",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DisasterDayOffPaidDefault",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "DisasterAdjustedHours",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "County",
                table: "Departments");
        }
    }
}
