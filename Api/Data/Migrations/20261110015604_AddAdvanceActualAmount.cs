using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvanceActualAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualAmount",
                table: "TravelRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualAmount",
                table: "AdvanceRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualCashTotal",
                table: "AdvanceRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualCheckTotal",
                table: "AdvanceRequests",
                type: "decimal(18,2)",
                nullable: true);

            // 回填：既有已結案的單，以已核准沖銷合計寫入實際金額（結案時觸發的那張沖銷於當時已轉 approved）
            migrationBuilder.Sql("""
                UPDATE a SET
                    a.ActualAmount     = ISNULL((SELECT SUM(w.GrandTotal) FROM WriteOffRecords w
                                                 WHERE w.AdvanceRequestId = a.Id AND w.ApprovalStatus = 'approved'), 0),
                    a.ActualCashTotal  = ISNULL((SELECT SUM(i.CashAmount) FROM WriteOffItems i
                                                 JOIN WriteOffRecords w ON w.Id = i.WriteOffRecordId
                                                 WHERE w.AdvanceRequestId = a.Id AND w.ApprovalStatus = 'approved'), 0),
                    a.ActualCheckTotal = ISNULL((SELECT SUM(i.CheckAmount) FROM WriteOffItems i
                                                 JOIN WriteOffRecords w ON w.Id = i.WriteOffRecordId
                                                 WHERE w.AdvanceRequestId = a.Id AND w.ApprovalStatus = 'approved'), 0)
                FROM AdvanceRequests a
                WHERE a.IsClosed = 1 AND a.ActualAmount IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE t SET
                    t.ActualAmount = ISNULL((SELECT SUM(w.GrandTotal) FROM TravelWriteOffRecords w
                                             WHERE w.TravelRequestId = t.Id AND w.ApprovalStatus = 'approved'), 0)
                FROM TravelRequests t
                WHERE t.IsClosed = 1 AND t.ActualAmount IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualAmount",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "ActualAmount",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "ActualCashTotal",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "ActualCheckTotal",
                table: "AdvanceRequests");
        }
    }
}
