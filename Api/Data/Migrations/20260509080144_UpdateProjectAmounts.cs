using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProjectAmounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 移除手動輸入的 ReceivedAmount（實收金額改為 SUM(ProjectPaymentSchedules.DepositAmount) 自動計算）
            migrationBuilder.DropColumn(
                name: "ReceivedAmount",
                table: "Projects");

            // 新增 RemainingAmount（剩餘金額；系統導入時舊專案的契約剩餘預算，選填）
            migrationBuilder.AddColumn<decimal>(
                name: "RemainingAmount",
                table: "Projects",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemainingAmount",
                table: "Projects");

            migrationBuilder.AddColumn<decimal>(
                name: "ReceivedAmount",
                table: "Projects",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReceivedAmount",
                value: 500000m);

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 2,
                column: "ReceivedAmount",
                value: 1200000m);

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 3,
                column: "ReceivedAmount",
                value: 300000m);

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 4,
                column: "ReceivedAmount",
                value: 2500000m);

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 5,
                column: "ReceivedAmount",
                value: 5200000m);

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 6,
                column: "ReceivedAmount",
                value: 2485000m);

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 7,
                column: "ReceivedAmount",
                value: 6852000m);

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 8,
                column: "ReceivedAmount",
                value: 6525000m);
        }
    }
}
