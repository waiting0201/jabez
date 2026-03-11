using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEscalated",
                table: "ApprovalRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OnBehalfOfUserId",
                table: "ApprovalRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EscalationOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OnBehalfOfUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalationOverrides_Users_OnBehalfOfUserId",
                        column: x => x.OnBehalfOfUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EscalationOverrides_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRecords_OnBehalfOfUserId",
                table: "ApprovalRecords",
                column: "OnBehalfOfUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationOverrides_ApplicationType_ApplicationId_StepOrder",
                table: "EscalationOverrides",
                columns: new[] { "ApplicationType", "ApplicationId", "StepOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalationOverrides_OnBehalfOfUserId",
                table: "EscalationOverrides",
                column: "OnBehalfOfUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationOverrides_ReviewerId",
                table: "EscalationOverrides",
                column: "ReviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRecords_Users_OnBehalfOfUserId",
                table: "ApprovalRecords",
                column: "OnBehalfOfUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRecords_Users_OnBehalfOfUserId",
                table: "ApprovalRecords");

            migrationBuilder.DropTable(
                name: "EscalationOverrides");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRecords_OnBehalfOfUserId",
                table: "ApprovalRecords");

            migrationBuilder.DropColumn(
                name: "IsEscalated",
                table: "ApprovalRecords");

            migrationBuilder.DropColumn(
                name: "OnBehalfOfUserId",
                table: "ApprovalRecords");
        }
    }
}
