using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWriteOffInstallmentsAndCheckPaid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CheckPaid",
                table: "WriteOffItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckPaidAt",
                table: "WriteOffItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CheckPaidById",
                table: "WriteOffItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WriteOffInstallments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WriteOffRecordId = table.Column<int>(type: "int", nullable: false),
                    InstallmentNo = table.Column<int>(type: "int", nullable: false),
                    ExpectedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaidByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WriteOffInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WriteOffInstallments_Users_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WriteOffInstallments_WriteOffRecords_WriteOffRecordId",
                        column: x => x.WriteOffRecordId,
                        principalTable: "WriteOffRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WriteOffItems_CheckPaidById",
                table: "WriteOffItems",
                column: "CheckPaidById");

            migrationBuilder.CreateIndex(
                name: "IX_WriteOffInstallments_PaidAt",
                table: "WriteOffInstallments",
                column: "PaidAt");

            migrationBuilder.CreateIndex(
                name: "IX_WriteOffInstallments_PaidByUserId",
                table: "WriteOffInstallments",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WriteOffInstallments_WriteOffRecordId_InstallmentNo",
                table: "WriteOffInstallments",
                columns: new[] { "WriteOffRecordId", "InstallmentNo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WriteOffItems_Users_CheckPaidById",
                table: "WriteOffItems",
                column: "CheckPaidById",
                principalTable: "Users",
                principalColumn: "Id");

            // Backfill：把既有「AdvanceRequest 上單一組退款日 / 退款金額」轉成該預支單
            // 最後一張已核准沖銷單的第 1 期分期，讓舊資料在新 UI 下仍看得到撥款進度。
            // 對應欄位：RefundAmount → Amount、EstimatedRefundDate → ExpectedDate、
            //           RefundedAt → PaidAt、RefundedByUserId → PaidByUserId。
            migrationBuilder.Sql("""
                INSERT INTO WriteOffInstallments
                    (WriteOffRecordId, InstallmentNo, ExpectedDate, PaidAt, Amount, Note, PaidByUserId, CreatedAt, UpdatedAt)
                SELECT  lastWo.Id,
                        1,
                        ISNULL(ar.EstimatedRefundDate, ar.RefundedAt),
                        ar.RefundedAt,
                        ar.RefundAmount,
                        N'系統轉檔：原單一退款日資料',
                        ar.RefundedByUserId,
                        SYSUTCDATETIME(),
                        SYSUTCDATETIME()
                FROM AdvanceRequests ar
                CROSS APPLY (
                    SELECT TOP 1 w.Id
                    FROM WriteOffRecords w
                    WHERE w.AdvanceRequestId = ar.Id AND w.ApprovalStatus = 'approved'
                    ORDER BY w.WriteOffNo DESC, w.Id DESC
                ) AS lastWo
                WHERE ar.RefundAmount > 0
                  AND (ar.EstimatedRefundDate IS NOT NULL OR ar.RefundedAt IS NOT NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WriteOffItems_Users_CheckPaidById",
                table: "WriteOffItems");

            migrationBuilder.DropTable(
                name: "WriteOffInstallments");

            migrationBuilder.DropIndex(
                name: "IX_WriteOffItems_CheckPaidById",
                table: "WriteOffItems");

            migrationBuilder.DropColumn(
                name: "CheckPaid",
                table: "WriteOffItems");

            migrationBuilder.DropColumn(
                name: "CheckPaidAt",
                table: "WriteOffItems");

            migrationBuilder.DropColumn(
                name: "CheckPaidById",
                table: "WriteOffItems");
        }
    }
}
