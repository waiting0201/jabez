using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmittedAtToRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "WriteOffRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "TravelWriteOffRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "TravelRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "TravelPaymentRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "PreReviewRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "PaymentRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "OvertimeRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "LeaveRevocations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "LeaveRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "AdvanceRequests",
                type: "datetime2",
                nullable: true);

            // 既有資料回填：已送簽（非草稿）的單，送簽日以建單日遞補 —— 這是唯一取得的到的近似值，
            // 且能保證改版前後所有歷史單據的「申請日期」顯示與報表數字完全不變。
            // 草稿刻意保持 NULL：它們本來就還沒有送簽日期，前端顯示「—」。
            migrationBuilder.Sql(
                "UPDATE PaymentRequests SET SubmittedAt = CreatedAt WHERE ApprovalStatus <> 'draft';");

            migrationBuilder.Sql(
                "UPDATE PreReviewRequests SET SubmittedAt = CreatedAt WHERE ApprovalStatus <> 'draft';");

            migrationBuilder.Sql(
                "UPDATE LeaveRequests SET SubmittedAt = CreatedAt WHERE ApprovalStatus <> 'draft';");

            migrationBuilder.Sql(
                "UPDATE LeaveRevocations SET SubmittedAt = CreatedAt WHERE ApprovalStatus <> 'draft';");

            migrationBuilder.Sql(
                "UPDATE TravelRequests SET SubmittedAt = CreatedAt WHERE ApprovalStatus <> 'draft';");

            migrationBuilder.Sql(
                "UPDATE TravelPaymentRequests SET SubmittedAt = CreatedAt WHERE ApprovalStatus <> 'draft';");

            migrationBuilder.Sql(
                "UPDATE OvertimeRequests SET SubmittedAt = CreatedAt WHERE ApprovalStatus <> 'draft';");

            migrationBuilder.Sql(
                "UPDATE AdvanceRequests SET SubmittedAt = CreatedAt WHERE ApprovalStatus <> 'draft';");

            migrationBuilder.Sql(
                "UPDATE WriteOffRecords SET SubmittedAt = CreatedAt WHERE ApprovalStatus <> 'draft';");

            migrationBuilder.Sql(
                "UPDATE TravelWriteOffRecords SET SubmittedAt = CreatedAt WHERE ApprovalStatus <> 'draft';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "WriteOffRecords");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "TravelWriteOffRecords");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "TravelPaymentRequests");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "PreReviewRequests");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "LeaveRevocations");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "AdvanceRequests");
        }
    }
}
