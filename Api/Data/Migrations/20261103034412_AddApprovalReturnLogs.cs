using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalReturnLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalReturnLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    RoundNo = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReviewerJobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReviewerDepartmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OnBehalfOfName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsEscalated = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalReturnLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalReturnLogs_ApplicationType_ApplicationId",
                table: "ApprovalReturnLogs",
                columns: new[] { "ApplicationType", "ApplicationId" });

            // 回填：尚存於 ApprovalRecords 的退回 / 拒絕紀錄（目前停在 returned 的單、已拒絕的單）。
            // 已重送過的單其舊紀錄早被清掉，無從回填。
            migrationBuilder.Sql("""
                INSERT INTO ApprovalReturnLogs
                    (ApplicationType, ApplicationId, StepOrder, RoundNo, Action, ReviewedById,
                     ReviewerName, ReviewerJobTitle, ReviewerDepartmentName, OnBehalfOfName,
                     IsEscalated, ReviewedAt, ReviewNote)
                SELECT ar.ApplicationType, ar.ApplicationId, ar.StepOrder, ar.RoundNo, ar.Action, ar.ReviewedById,
                       ISNULL(u.Name, N''), jt.Name, dep.Name, obu.Name,
                       ar.IsEscalated, ar.ReviewedAt, ar.ReviewNote
                FROM ApprovalRecords ar
                LEFT JOIN Users u         ON ar.ReviewedById     = u.Id
                LEFT JOIN Users obu       ON ar.OnBehalfOfUserId = obu.Id
                LEFT JOIN JobTitles jt    ON u.JobTitleId        = jt.Id
                LEFT JOIN Departments dep ON u.DepartmentId      = dep.Id
                WHERE ar.Action IN ('returned', 'rejected');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalReturnLogs");
        }
    }
}
