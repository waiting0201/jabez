using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentInstallments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdvanceRequestInstallments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdvanceRequestId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AdvanceRequestInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdvanceRequestInstallments_AdvanceRequests_AdvanceRequestId",
                        column: x => x.AdvanceRequestId,
                        principalTable: "AdvanceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdvanceRequestInstallments_Users_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PaymentRequestInstallments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentRequestId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PaymentRequestInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRequestInstallments_PaymentRequests_PaymentRequestId",
                        column: x => x.PaymentRequestId,
                        principalTable: "PaymentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentRequestInstallments_Users_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TravelPaymentRequestInstallments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TravelPaymentRequestId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TravelPaymentRequestInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelPaymentRequestInstallments_TravelPaymentRequests_TravelPaymentRequestId",
                        column: x => x.TravelPaymentRequestId,
                        principalTable: "TravelPaymentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TravelPaymentRequestInstallments_Users_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TravelRequestInstallments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TravelRequestId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TravelRequestInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelRequestInstallments_TravelRequests_TravelRequestId",
                        column: x => x.TravelRequestId,
                        principalTable: "TravelRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TravelRequestInstallments_Users_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequestInstallments_AdvanceRequestId_InstallmentNo",
                table: "AdvanceRequestInstallments",
                columns: new[] { "AdvanceRequestId", "InstallmentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequestInstallments_PaidAt",
                table: "AdvanceRequestInstallments",
                column: "PaidAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequestInstallments_PaidByUserId",
                table: "AdvanceRequestInstallments",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequestInstallments_PaidAt",
                table: "PaymentRequestInstallments",
                column: "PaidAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequestInstallments_PaidByUserId",
                table: "PaymentRequestInstallments",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequestInstallments_PaymentRequestId_InstallmentNo",
                table: "PaymentRequestInstallments",
                columns: new[] { "PaymentRequestId", "InstallmentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelPaymentRequestInstallments_PaidAt",
                table: "TravelPaymentRequestInstallments",
                column: "PaidAt");

            migrationBuilder.CreateIndex(
                name: "IX_TravelPaymentRequestInstallments_PaidByUserId",
                table: "TravelPaymentRequestInstallments",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelPaymentRequestInstallments_TravelPaymentRequestId_InstallmentNo",
                table: "TravelPaymentRequestInstallments",
                columns: new[] { "TravelPaymentRequestId", "InstallmentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequestInstallments_PaidAt",
                table: "TravelRequestInstallments",
                column: "PaidAt");

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequestInstallments_PaidByUserId",
                table: "TravelRequestInstallments",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequestInstallments_TravelRequestId_InstallmentNo",
                table: "TravelRequestInstallments",
                columns: new[] { "TravelRequestId", "InstallmentNo" },
                unique: true);

            // 將既有單筆撥款資料 backfill 為 InstallmentNo=1 的分期紀錄
            // 父表 EstimatedPaymentDate / PaidAt 仍保留作為 cache，由 Handler 維持同步
            migrationBuilder.Sql(@"
INSERT INTO PaymentRequestInstallments
  (PaymentRequestId, InstallmentNo, ExpectedDate, PaidAt, Amount,
   PaidByUserId, CreatedAt, UpdatedAt)
SELECT Id, 1,
       COALESCE(EstimatedPaymentDate, PaidAt),
       PaidAt, TotalAmount, PaidByUserId, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM PaymentRequests
WHERE PaidAt IS NOT NULL OR EstimatedPaymentDate IS NOT NULL;");

            migrationBuilder.Sql(@"
INSERT INTO AdvanceRequestInstallments
  (AdvanceRequestId, InstallmentNo, ExpectedDate, PaidAt, Amount,
   PaidByUserId, CreatedAt, UpdatedAt)
SELECT Id, 1,
       COALESCE(EstimatedPaymentDate, PaidAt),
       PaidAt, GrandTotal, PaidByUserId, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM AdvanceRequests
WHERE PaidAt IS NOT NULL OR EstimatedPaymentDate IS NOT NULL;");

            migrationBuilder.Sql(@"
INSERT INTO TravelRequestInstallments
  (TravelRequestId, InstallmentNo, ExpectedDate, PaidAt, Amount,
   PaidByUserId, CreatedAt, UpdatedAt)
SELECT Id, 1,
       COALESCE(EstimatedPaymentDate, PaidAt),
       PaidAt, GrandTotal, PaidByUserId, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM TravelRequests
WHERE PaidAt IS NOT NULL OR EstimatedPaymentDate IS NOT NULL;");

            migrationBuilder.Sql(@"
INSERT INTO TravelPaymentRequestInstallments
  (TravelPaymentRequestId, InstallmentNo, ExpectedDate, PaidAt, Amount,
   PaidByUserId, CreatedAt, UpdatedAt)
SELECT Id, 1,
       COALESCE(EstimatedPaymentDate, PaidAt),
       PaidAt, GrandTotal, PaidByUserId, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM TravelPaymentRequests
WHERE PaidAt IS NOT NULL OR EstimatedPaymentDate IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvanceRequestInstallments");

            migrationBuilder.DropTable(
                name: "PaymentRequestInstallments");

            migrationBuilder.DropTable(
                name: "TravelPaymentRequestInstallments");

            migrationBuilder.DropTable(
                name: "TravelRequestInstallments");
        }
    }
}
