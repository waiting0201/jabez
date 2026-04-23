using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameProjectAmountsAndAddPaymentSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BudgetAmount",
                table: "Projects",
                newName: "ReceivedAmount");

            migrationBuilder.RenameColumn(
                name: "ActualAmount",
                table: "Projects",
                newName: "ContractAmount");

            migrationBuilder.CreateTable(
                name: "ProjectPaymentSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    PeriodNo = table.Column<int>(type: "int", nullable: false),
                    BillingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BillingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvoiceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DepositDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DepositAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeductionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPaymentSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPaymentSchedules_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPaymentSchedules_ProjectId_PeriodNo",
                table: "ProjectPaymentSchedules",
                columns: new[] { "ProjectId", "PeriodNo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectPaymentSchedules");

            migrationBuilder.RenameColumn(
                name: "ReceivedAmount",
                table: "Projects",
                newName: "BudgetAmount");

            migrationBuilder.RenameColumn(
                name: "ContractAmount",
                table: "Projects",
                newName: "ActualAmount");
        }
    }
}
