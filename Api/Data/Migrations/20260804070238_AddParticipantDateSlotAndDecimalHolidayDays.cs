using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantDateSlotAndDecimalHolidayDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // int → decimal(5,1)：SQL Server 為隱含放大轉換，既有整數天數原封保留（3 → 3.0）
            migrationBuilder.AlterColumn<decimal>(
                name: "HolidayDays",
                table: "TravelRequestParticipants",
                type: "decimal(5,1)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slot",
                table: "TravelRequestParticipantDates",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "full");   // 既有列一律視為全天
        }

        /// <inheritdoc />
        /// <remarks>
        /// ⚠️ 降級有損：decimal(5,1) → int 在 SQL Server 是截斷（1.5 → 1），
        /// 回滾前須先確認 TravelRequestParticipants.HolidayDays 無 0.5 的資料。
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Slot",
                table: "TravelRequestParticipantDates");

            migrationBuilder.AlterColumn<int>(
                name: "HolidayDays",
                table: "TravelRequestParticipants",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,1)",
                oldNullable: true);
        }
    }
}
