using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalStepDesignatedJobTitles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalStepDesignatedJobTitles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovalStepId = table.Column<int>(type: "int", nullable: false),
                    JobTitleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalStepDesignatedJobTitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalStepDesignatedJobTitles_ApprovalSteps_ApprovalStepId",
                        column: x => x.ApprovalStepId,
                        principalTable: "ApprovalSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalStepDesignatedJobTitles_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStepDesignatedJobTitles_ApprovalStepId_JobTitleId",
                table: "ApprovalStepDesignatedJobTitles",
                columns: new[] { "ApprovalStepId", "JobTitleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStepDesignatedJobTitles_JobTitleId",
                table: "ApprovalStepDesignatedJobTitles",
                column: "JobTitleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalStepDesignatedJobTitles");
        }
    }
}
