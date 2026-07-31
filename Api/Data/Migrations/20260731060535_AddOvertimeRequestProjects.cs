using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOvertimeRequestProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OvertimeRequestProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OvertimeRequestId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    EstimatedHours = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OvertimeRequestProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OvertimeRequestProjects_OvertimeRequests_OvertimeRequestId",
                        column: x => x.OvertimeRequestId,
                        principalTable: "OvertimeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OvertimeRequestProjects_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequestProjects_OvertimeRequestId_ProjectId",
                table: "OvertimeRequestProjects",
                columns: new[] { "OvertimeRequestId", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequestProjects_ProjectId",
                table: "OvertimeRequestProjects",
                column: "ProjectId");

            // ── 舊資料回填：CSV OvertimeRequests.ProjectIds → 明細列 ────────────────
            // 1 個專案   → 該案時數 = 原總時數
            // N 個專案   → 平均分配，餘數（以 0.1 小時為最小單位）補到第一列，確保 SUM = 父表 EstimatedHours
            // 無專案     → 不建明細，父表 EstimatedHours 原樣保留
            // 精度：全程以「十分之一小時」整數做除法，避免 5.0/3 被 decimal(5,1) 截斷後合計對不上。
            //       驗算 5.0h/3 → 1.8+1.6+1.6=5.0；2.5h/2 → 1.3+1.2=2.5；1.0h/3 → 0.4+0.3+0.3=1.0
            // CSV 內可能殘留已刪除的 ProjectId（舊欄位無 FK 約束），以 EXISTS 跳過，
            // 否則 NoAction FK 會讓整個 migration 失敗。
            // ProjectIds 欄位本身在後續 migration RemoveOvertimeRequestProjectIdsCsv 才 DROP，
            // 本 migration 的 Down() 因此可無損回退。
            migrationBuilder.Sql("""
                WITH Parsed AS (
                    SELECT o.Id AS OvertimeRequestId,
                           o.EstimatedHours,
                           CAST(j.[value] AS INT) AS ProjectId,
                           CAST(j.[key]   AS INT) AS Ordinal
                    FROM OvertimeRequests o
                    CROSS APPLY OPENJSON('[' + o.ProjectIds + ']') j
                    WHERE o.ProjectIds IS NOT NULL AND LTRIM(RTRIM(o.ProjectIds)) <> ''
                ),
                Valid AS (
                    SELECT p.OvertimeRequestId, p.ProjectId, p.EstimatedHours, MIN(p.Ordinal) AS Ordinal
                    FROM Parsed p
                    WHERE EXISTS (SELECT 1 FROM Projects pr WHERE pr.Id = p.ProjectId)
                    GROUP BY p.OvertimeRequestId, p.ProjectId, p.EstimatedHours
                ),
                Ranked AS (
                    SELECT v.OvertimeRequestId, v.ProjectId,
                           ROW_NUMBER() OVER (PARTITION BY v.OvertimeRequestId ORDER BY v.Ordinal) AS Rn,
                           COUNT(*)     OVER (PARTITION BY v.OvertimeRequestId)                    AS Cnt,
                           CAST(ROUND(v.EstimatedHours * 10, 0) AS INT)                            AS TotalTenths
                    FROM Valid v
                )
                INSERT INTO OvertimeRequestProjects (OvertimeRequestId, ProjectId, EstimatedHours, SortOrder)
                SELECT r.OvertimeRequestId,
                       r.ProjectId,
                       CAST(((r.TotalTenths / r.Cnt) + CASE WHEN r.Rn = 1 THEN r.TotalTenths % r.Cnt ELSE 0 END) / 10.0 AS DECIMAL(5,1)),
                       r.Rn - 1
                FROM Ranked r;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OvertimeRequestProjects");
        }
    }
}
