using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestNoToPaymentTravelTravelPaymentRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 1: 新增可空欄位（避開既有資料 NOT NULL 衝突）
            migrationBuilder.AddColumn<string>(
                name: "RequestNo",
                table: "TravelRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestNo",
                table: "TravelPaymentRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestNo",
                table: "PaymentRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // Phase 2: 補號（per-prefix-per-day 序號池，依 CreatedAt + Id 為穩定排序）
            // PaymentRequests → PR-yyyyMMdd-NNN
            migrationBuilder.Sql("""
                WITH Numbered AS (
                    SELECT Id, CreatedAt,
                           ROW_NUMBER() OVER (
                               PARTITION BY CAST(CreatedAt AS DATE)
                               ORDER BY CreatedAt, Id
                           ) AS Seq
                    FROM PaymentRequests
                )
                UPDATE pr
                SET RequestNo = 'PR-' + FORMAT(n.CreatedAt, 'yyyyMMdd') + '-' + FORMAT(n.Seq, '000')
                FROM PaymentRequests pr
                JOIN Numbered n ON pr.Id = n.Id;
                """);

            // TravelPaymentRequests → TPR-yyyyMMdd-NNN
            migrationBuilder.Sql("""
                WITH Numbered AS (
                    SELECT Id, CreatedAt,
                           ROW_NUMBER() OVER (
                               PARTITION BY CAST(CreatedAt AS DATE)
                               ORDER BY CreatedAt, Id
                           ) AS Seq
                    FROM TravelPaymentRequests
                )
                UPDATE tpr
                SET RequestNo = 'TPR-' + FORMAT(n.CreatedAt, 'yyyyMMdd') + '-' + FORMAT(n.Seq, '000')
                FROM TravelPaymentRequests tpr
                JOIN Numbered n ON tpr.Id = n.Id;
                """);

            // TravelRequests：IsHolidayTravel=0 → TR-、IsHolidayTravel=1 → HTR-（per-prefix-per-day 序號池獨立）
            migrationBuilder.Sql("""
                WITH Numbered AS (
                    SELECT Id, IsHolidayTravel, CreatedAt,
                           CASE WHEN IsHolidayTravel = 1 THEN 'HTR-' ELSE 'TR-' END AS Prefix,
                           ROW_NUMBER() OVER (
                               PARTITION BY CAST(CreatedAt AS DATE), IsHolidayTravel
                               ORDER BY CreatedAt, Id
                           ) AS Seq
                    FROM TravelRequests
                )
                UPDATE tr
                SET RequestNo = n.Prefix + FORMAT(n.CreatedAt, 'yyyyMMdd') + '-' + FORMAT(n.Seq, '000')
                FROM TravelRequests tr
                JOIN Numbered n ON tr.Id = n.Id;
                """);

            // Phase 3: 改 NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "TravelRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "TravelPaymentRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "PaymentRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            // Phase 4: 加 unique index
            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_RequestNo",
                table: "TravelRequests",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelPaymentRequests_RequestNo",
                table: "TravelPaymentRequests",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_RequestNo",
                table: "PaymentRequests",
                column: "RequestNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TravelRequests_RequestNo",
                table: "TravelRequests");

            migrationBuilder.DropIndex(
                name: "IX_TravelPaymentRequests_RequestNo",
                table: "TravelPaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_RequestNo",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "RequestNo",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "RequestNo",
                table: "TravelPaymentRequests");

            migrationBuilder.DropColumn(
                name: "RequestNo",
                table: "PaymentRequests");
        }
    }
}
