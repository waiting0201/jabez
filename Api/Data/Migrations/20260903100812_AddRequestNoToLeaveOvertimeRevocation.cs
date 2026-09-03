using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestNoToLeaveOvertimeRevocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestNo",
                table: "OvertimeRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestNo",
                table: "LeaveRevocations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestNo",
                table: "LeaveRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequests_RequestNo",
                table: "OvertimeRequests",
                column: "RequestNo",
                unique: true,
                filter: "[RequestNo] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRevocations_RequestNo",
                table: "LeaveRevocations",
                column: "RequestNo",
                unique: true,
                filter: "[RequestNo] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_RequestNo",
                table: "LeaveRequests",
                column: "RequestNo",
                unique: true,
                filter: "[RequestNo] IS NOT NULL");

            // 既有已送簽的單補號（草稿不補，維持「送簽才取號」語意）。
            // 日期取 COALESCE(SubmittedAt, CreatedAt)，當日流水號以 Id 排序，
            // 與 RequestNoGenerator 的 {prefix}yyyyMMdd-NNN 格式一致，
            // 依日期分組編號故不會撞上 filtered unique index。
            // 比照 AddSubmittedAtToRequests 的補值作法，否則舊單的單號欄永遠是空的。
            migrationBuilder.Sql(BackfillSql("LeaveRequests",    "LV-"));
            migrationBuilder.Sql(BackfillSql("OvertimeRequests", "OT-"));
            migrationBuilder.Sql(BackfillSql("LeaveRevocations", "LVR-"));
        }

        private static string BackfillSql(string table, string prefix) => $"""
            WITH numbered AS (
                SELECT Id,
                       CONVERT(varchar(8), COALESCE(SubmittedAt, CreatedAt), 112) AS Ymd,
                       ROW_NUMBER() OVER (
                           PARTITION BY CONVERT(varchar(8), COALESCE(SubmittedAt, CreatedAt), 112)
                           ORDER BY Id) AS Seq
                FROM {table}
                WHERE ApprovalStatus <> 'draft' AND RequestNo IS NULL
            )
            UPDATE t
            SET RequestNo = '{prefix}' + n.Ymd + '-' +
                CASE WHEN n.Seq < 1000
                     THEN RIGHT('000' + CAST(n.Seq AS varchar(10)), 3)
                     ELSE CAST(n.Seq AS varchar(10)) END
            FROM {table} t
            JOIN numbered n ON t.Id = n.Id;
            """;

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OvertimeRequests_RequestNo",
                table: "OvertimeRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRevocations_RequestNo",
                table: "LeaveRevocations");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_RequestNo",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "RequestNo",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "RequestNo",
                table: "LeaveRevocations");

            migrationBuilder.DropColumn(
                name: "RequestNo",
                table: "LeaveRequests");
        }
    }
}
