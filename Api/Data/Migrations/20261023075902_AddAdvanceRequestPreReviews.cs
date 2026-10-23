using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvanceRequestPreReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdvanceRequestPreReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdvanceRequestId = table.Column<int>(type: "int", nullable: false),
                    PreReviewRequestId = table.Column<int>(type: "int", nullable: false),
                    RoundNo = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvanceRequestPreReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdvanceRequestPreReviews_AdvanceRequests_AdvanceRequestId",
                        column: x => x.AdvanceRequestId,
                        principalTable: "AdvanceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdvanceRequestPreReviews_PreReviewRequests_PreReviewRequestId",
                        column: x => x.PreReviewRequestId,
                        principalTable: "PreReviewRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequestPreReviews_AdvanceRequestId_PreReviewRequestId",
                table: "AdvanceRequestPreReviews",
                columns: new[] { "AdvanceRequestId", "PreReviewRequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequestPreReviews_PreReviewRequestId",
                table: "AdvanceRequestPreReviews",
                column: "PreReviewRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvanceRequestPreReviews");
        }
    }
}
