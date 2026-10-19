using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOvertimeRequestKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualEndTime",
                table: "OvertimeRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStartTime",
                table: "OvertimeRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestKind",
                table: "OvertimeRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "normal");

            // 回填：原「補加班」＝加班日早於申請日（已送簽比送簽日、草稿比今天），改為落欄位 post_hoc。
            // 草稿無 SubmittedAt → 以台北現在時間（DB 為 UTC，+8h；Clock.Now 寫入的 SubmittedAt 本身即台北 naive）。
            // 舊單無 Actual* 起訖（保持 NULL），核准流程遇 null 起訖會跳過自動寫入，維持現狀。
            migrationBuilder.Sql(@"
UPDATE OvertimeRequests
SET RequestKind = 'post_hoc'
WHERE CAST(OvertimeDate AS date) < CAST(COALESCE(SubmittedAt, DATEADD(hour, 8, GETUTCDATE())) AS date);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualEndTime",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "ActualStartTime",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "RequestKind",
                table: "OvertimeRequests");
        }
    }
}
