using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPreReviewRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreReviewRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    ApprovalItemId = table.Column<int>(type: "int", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    CurrentStepOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SubmittedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreReviewRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreReviewRequests_ApprovalItems_ApprovalItemId",
                        column: x => x.ApprovalItemId,
                        principalTable: "ApprovalItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PreReviewRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreReviewRequests_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreReviewRequests_Users_SubmittedById",
                        column: x => x.SubmittedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PreReviewRequests_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PreReviewItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PreReviewRequestId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ItemCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ItemDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreReviewItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreReviewItems_PreReviewRequests_PreReviewRequestId",
                        column: x => x.PreReviewRequestId,
                        principalTable: "PreReviewRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreReviewRequestAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PreReviewRequestId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreReviewRequestAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreReviewRequestAttachments_PreReviewRequests_PreReviewRequestId",
                        column: x => x.PreReviewRequestId,
                        principalTable: "PreReviewRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { "75", "pre-review-requests:read", "預審申請", "預審申請", "瀏覽" },
                    { "76", "pre-review-requests:write", "預審申請", "預審申請", "新增/修改" },
                    { "77", "pre-review-requests:delete", "預審申請", "預審申請", "刪除" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreReviewItems_PreReviewRequestId",
                table: "PreReviewItems",
                column: "PreReviewRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PreReviewRequestAttachments_PreReviewRequestId",
                table: "PreReviewRequestAttachments",
                column: "PreReviewRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PreReviewRequests_ApprovalItemId",
                table: "PreReviewRequests",
                column: "ApprovalItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PreReviewRequests_ProjectId",
                table: "PreReviewRequests",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PreReviewRequests_RequestNo",
                table: "PreReviewRequests",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreReviewRequests_ReviewedById",
                table: "PreReviewRequests",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_PreReviewRequests_SubmittedById",
                table: "PreReviewRequests",
                column: "SubmittedById");

            migrationBuilder.CreateIndex(
                name: "IX_PreReviewRequests_VendorId",
                table: "PreReviewRequests",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreReviewItems");

            migrationBuilder.DropTable(
                name: "PreReviewRequestAttachments");

            migrationBuilder.DropTable(
                name: "PreReviewRequests");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "75");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "76");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "77");
        }
    }
}
