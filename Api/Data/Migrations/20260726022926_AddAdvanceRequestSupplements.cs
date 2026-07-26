using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvanceRequestSupplements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdvanceRequestItems_AdvanceRequestId",
                table: "AdvanceRequestItems");

            migrationBuilder.AddColumn<int>(
                name: "RoundNo",
                table: "ApprovalRecords",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CurrentRoundNo",
                table: "AdvanceRequests",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "RoundNo",
                table: "AdvanceRequestItems",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "AdvanceRequestSupplements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdvanceRequestId = table.Column<int>(type: "int", nullable: false),
                    RoundNo = table.Column<int>(type: "int", nullable: false),
                    AdvanceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrevCurrentStepOrder = table.Column<int>(type: "int", nullable: false),
                    PrevReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PrevReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PrevReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvanceRequestSupplements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdvanceRequestSupplements_AdvanceRequests_AdvanceRequestId",
                        column: x => x.AdvanceRequestId,
                        principalTable: "AdvanceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdvanceRequestSupplements_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AdvanceRequestSupplements_Users_PrevReviewedById",
                        column: x => x.PrevReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequestItems_AdvanceRequestId_RoundNo",
                table: "AdvanceRequestItems",
                columns: new[] { "AdvanceRequestId", "RoundNo" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequestSupplements_AdvanceRequestId_RoundNo",
                table: "AdvanceRequestSupplements",
                columns: new[] { "AdvanceRequestId", "RoundNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequestSupplements_CreatedById",
                table: "AdvanceRequestSupplements",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequestSupplements_PrevReviewedById",
                table: "AdvanceRequestSupplements",
                column: "PrevReviewedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvanceRequestSupplements");

            migrationBuilder.DropIndex(
                name: "IX_AdvanceRequestItems_AdvanceRequestId_RoundNo",
                table: "AdvanceRequestItems");

            migrationBuilder.DropColumn(
                name: "RoundNo",
                table: "ApprovalRecords");

            migrationBuilder.DropColumn(
                name: "CurrentRoundNo",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "RoundNo",
                table: "AdvanceRequestItems");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequestItems_AdvanceRequestId",
                table: "AdvanceRequestItems",
                column: "AdvanceRequestId");
        }
    }
}
