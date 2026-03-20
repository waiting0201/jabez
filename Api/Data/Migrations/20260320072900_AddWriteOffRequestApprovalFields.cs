using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWriteOffRequestApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalItemId",
                table: "WriteOffRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "WriteOffRecords",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "draft");

            migrationBuilder.AddColumn<int>(
                name: "CurrentStepOrder",
                table: "WriteOffRecords",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "RequestNo",
                table: "WriteOffRecords",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "WriteOffRecords",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "WriteOffRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedById",
                table: "WriteOffRecords",
                type: "uniqueidentifier",
                nullable: true);

            // 為現有的 WriteOffRecords 補上唯一 RequestNo，再建立唯一索引
            migrationBuilder.Sql("""
                UPDATE WriteOffRecords
                SET RequestNo = 'WO-LEGACY-' + RIGHT('000' + CAST(Id AS NVARCHAR(10)), 3)
                WHERE RequestNo = '' OR RequestNo IS NULL
                """);

            migrationBuilder.CreateIndex(
                name: "IX_WriteOffRecords_RequestNo",
                table: "WriteOffRecords",
                column: "RequestNo",
                unique: true);

            migrationBuilder.InsertData(
                table: "ApprovalItems",
                columns: new[] { "Id", "ApplicationType", "Code", "CreatedAt", "Description", "IsActive", "Name" },
                values: new object[] { 7, "write_off", "write_off_request", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "沖銷申請" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { "54", "write-off-requests:read", null, "沖銷申請", "瀏覽" },
                    { "55", "write-off-requests:write", null, "沖銷申請", "新增/修改" },
                    { "56", "write-off-requests:delete", null, "沖銷申請", "刪除" }
                });

            migrationBuilder.InsertData(
                table: "ApprovalSteps",
                columns: new[] { "Id", "ApprovalItemId", "CreatedAt", "DepartmentId", "JobTitleId", "Note", "StepOrder", "UseApplicantDepartment" },
                values: new object[] { 20, 7, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, "部門主管初核", 1, true });

            migrationBuilder.InsertData(
                table: "ApprovalSteps",
                columns: new[] { "Id", "ApprovalItemId", "CreatedAt", "DepartmentId", "JobTitleId", "Note", "StepOrder" },
                values: new object[,]
                {
                    { 21, 7, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 4, "取得紙本資料審核", 2 },
                    { 22, 7, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 4, "填入預計撥款日，核決及撥款後，填入撥款日", 3 },
                    { 23, 7, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, 5, "最終核決", 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_WriteOffRecords_ApprovalItemId",
                table: "WriteOffRecords",
                column: "ApprovalItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WriteOffRecords_ReviewedById",
                table: "WriteOffRecords",
                column: "ReviewedById");

            migrationBuilder.AddForeignKey(
                name: "FK_WriteOffRecords_ApprovalItems_ApprovalItemId",
                table: "WriteOffRecords",
                column: "ApprovalItemId",
                principalTable: "ApprovalItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WriteOffRecords_Users_ReviewedById",
                table: "WriteOffRecords",
                column: "ReviewedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WriteOffRecords_ApprovalItems_ApprovalItemId",
                table: "WriteOffRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_WriteOffRecords_Users_ReviewedById",
                table: "WriteOffRecords");

            migrationBuilder.DropIndex(
                name: "IX_WriteOffRecords_RequestNo",
                table: "WriteOffRecords");

            migrationBuilder.DropIndex(
                name: "IX_WriteOffRecords_ApprovalItemId",
                table: "WriteOffRecords");

            migrationBuilder.DropIndex(
                name: "IX_WriteOffRecords_ReviewedById",
                table: "WriteOffRecords");

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "54");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "55");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "56");

            migrationBuilder.DeleteData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DropColumn(
                name: "ApprovalItemId",
                table: "WriteOffRecords");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "WriteOffRecords");

            migrationBuilder.DropColumn(
                name: "CurrentStepOrder",
                table: "WriteOffRecords");

            migrationBuilder.DropColumn(
                name: "RequestNo",
                table: "WriteOffRecords");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "WriteOffRecords");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "WriteOffRecords");

            migrationBuilder.DropColumn(
                name: "ReviewedById",
                table: "WriteOffRecords");
        }
    }
}
