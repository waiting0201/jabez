using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalStepOrderToDesignatedReviewer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RequestDesignatedReviewers_RequestType_RequestId_ReviewerId",
                table: "RequestDesignatedReviewers");

            migrationBuilder.DropIndex(
                name: "IX_RequestDesignatedReviewers_RequestType_RequestId_StepOrder",
                table: "RequestDesignatedReviewers");

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStepOrder",
                table: "RequestDesignatedReviewers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SelectedDepartmentId",
                table: "RequestDesignatedReviewers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DesignatedRequiresDepartment",
                table: "ApprovalSteps",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // 回填既有資料：舊流程都只有一個 designated step，將每張申請所有 designee 的
            // ApprovalStepOrder 設成「該申請 ApprovalItem 中唯一 UseApplicantDesignated step 的 StepOrder」。
            // 涵蓋全部 10 種 RequestType（travel / holiday_travel 共用 TravelRequests 表）。
            // 僅更新 ApprovalStepOrder = 0（剛新增的預設值）且該申請流程確有 designated step 者。
            migrationBuilder.Sql("""
                DECLARE @bf TABLE (RequestType nvarchar(30), TableName sysname);
                -- 對照表，逐一 UPDATE
                UPDATE rdr SET rdr.ApprovalStepOrder = ca.StepOrder
                FROM RequestDesignatedReviewers rdr
                JOIN PaymentRequests t ON t.Id = rdr.RequestId
                CROSS APPLY (SELECT MIN(s.StepOrder) AS StepOrder FROM ApprovalSteps s WHERE s.ApprovalItemId = t.ApprovalItemId AND s.UseApplicantDesignated = 1) ca
                WHERE rdr.RequestType = 'payment_request' AND rdr.ApprovalStepOrder = 0 AND ca.StepOrder IS NOT NULL;

                UPDATE rdr SET rdr.ApprovalStepOrder = ca.StepOrder
                FROM RequestDesignatedReviewers rdr
                JOIN AdvanceRequests t ON t.Id = rdr.RequestId
                CROSS APPLY (SELECT MIN(s.StepOrder) AS StepOrder FROM ApprovalSteps s WHERE s.ApprovalItemId = t.ApprovalItemId AND s.UseApplicantDesignated = 1) ca
                WHERE rdr.RequestType = 'advance' AND rdr.ApprovalStepOrder = 0 AND ca.StepOrder IS NOT NULL;

                UPDATE rdr SET rdr.ApprovalStepOrder = ca.StepOrder
                FROM RequestDesignatedReviewers rdr
                JOIN TravelRequests t ON t.Id = rdr.RequestId
                CROSS APPLY (SELECT MIN(s.StepOrder) AS StepOrder FROM ApprovalSteps s WHERE s.ApprovalItemId = t.ApprovalItemId AND s.UseApplicantDesignated = 1) ca
                WHERE rdr.RequestType = 'travel' AND rdr.ApprovalStepOrder = 0 AND ca.StepOrder IS NOT NULL;

                UPDATE rdr SET rdr.ApprovalStepOrder = ca.StepOrder
                FROM RequestDesignatedReviewers rdr
                JOIN TravelRequests t ON t.Id = rdr.RequestId
                CROSS APPLY (SELECT MIN(s.StepOrder) AS StepOrder FROM ApprovalSteps s WHERE s.ApprovalItemId = t.ApprovalItemId AND s.UseApplicantDesignated = 1) ca
                WHERE rdr.RequestType = 'holiday_travel' AND rdr.ApprovalStepOrder = 0 AND ca.StepOrder IS NOT NULL;

                UPDATE rdr SET rdr.ApprovalStepOrder = ca.StepOrder
                FROM RequestDesignatedReviewers rdr
                JOIN TravelPaymentRequests t ON t.Id = rdr.RequestId
                CROSS APPLY (SELECT MIN(s.StepOrder) AS StepOrder FROM ApprovalSteps s WHERE s.ApprovalItemId = t.ApprovalItemId AND s.UseApplicantDesignated = 1) ca
                WHERE rdr.RequestType = 'travel_payment' AND rdr.ApprovalStepOrder = 0 AND ca.StepOrder IS NOT NULL;

                UPDATE rdr SET rdr.ApprovalStepOrder = ca.StepOrder
                FROM RequestDesignatedReviewers rdr
                JOIN WriteOffRecords t ON t.Id = rdr.RequestId
                CROSS APPLY (SELECT MIN(s.StepOrder) AS StepOrder FROM ApprovalSteps s WHERE s.ApprovalItemId = t.ApprovalItemId AND s.UseApplicantDesignated = 1) ca
                WHERE rdr.RequestType = 'write_off' AND rdr.ApprovalStepOrder = 0 AND ca.StepOrder IS NOT NULL;

                UPDATE rdr SET rdr.ApprovalStepOrder = ca.StepOrder
                FROM RequestDesignatedReviewers rdr
                JOIN TravelWriteOffRecords t ON t.Id = rdr.RequestId
                CROSS APPLY (SELECT MIN(s.StepOrder) AS StepOrder FROM ApprovalSteps s WHERE s.ApprovalItemId = t.ApprovalItemId AND s.UseApplicantDesignated = 1) ca
                WHERE rdr.RequestType = 'travel_write_off' AND rdr.ApprovalStepOrder = 0 AND ca.StepOrder IS NOT NULL;

                UPDATE rdr SET rdr.ApprovalStepOrder = ca.StepOrder
                FROM RequestDesignatedReviewers rdr
                JOIN LeaveRequests t ON t.Id = rdr.RequestId
                CROSS APPLY (SELECT MIN(s.StepOrder) AS StepOrder FROM ApprovalSteps s WHERE s.ApprovalItemId = t.ApprovalItemId AND s.UseApplicantDesignated = 1) ca
                WHERE rdr.RequestType = 'leave' AND rdr.ApprovalStepOrder = 0 AND ca.StepOrder IS NOT NULL;

                UPDATE rdr SET rdr.ApprovalStepOrder = ca.StepOrder
                FROM RequestDesignatedReviewers rdr
                JOIN OvertimeRequests t ON t.Id = rdr.RequestId
                CROSS APPLY (SELECT MIN(s.StepOrder) AS StepOrder FROM ApprovalSteps s WHERE s.ApprovalItemId = t.ApprovalItemId AND s.UseApplicantDesignated = 1) ca
                WHERE rdr.RequestType = 'overtime' AND rdr.ApprovalStepOrder = 0 AND ca.StepOrder IS NOT NULL;

                UPDATE rdr SET rdr.ApprovalStepOrder = ca.StepOrder
                FROM RequestDesignatedReviewers rdr
                JOIN PreReviewRequests t ON t.Id = rdr.RequestId
                CROSS APPLY (SELECT MIN(s.StepOrder) AS StepOrder FROM ApprovalSteps s WHERE s.ApprovalItemId = t.ApprovalItemId AND s.UseApplicantDesignated = 1) ca
                WHERE rdr.RequestType = 'pre_review' AND rdr.ApprovalStepOrder = 0 AND ca.StepOrder IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_RequestDesignatedReviewers_RequestType_RequestId_ApprovalStepOrder_ReviewerId",
                table: "RequestDesignatedReviewers",
                columns: new[] { "RequestType", "RequestId", "ApprovalStepOrder", "ReviewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestDesignatedReviewers_RequestType_RequestId_ApprovalStepOrder_StepOrder",
                table: "RequestDesignatedReviewers",
                columns: new[] { "RequestType", "RequestId", "ApprovalStepOrder", "StepOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RequestDesignatedReviewers_RequestType_RequestId_ApprovalStepOrder_ReviewerId",
                table: "RequestDesignatedReviewers");

            migrationBuilder.DropIndex(
                name: "IX_RequestDesignatedReviewers_RequestType_RequestId_ApprovalStepOrder_StepOrder",
                table: "RequestDesignatedReviewers");

            migrationBuilder.DropColumn(
                name: "ApprovalStepOrder",
                table: "RequestDesignatedReviewers");

            migrationBuilder.DropColumn(
                name: "SelectedDepartmentId",
                table: "RequestDesignatedReviewers");

            migrationBuilder.DropColumn(
                name: "DesignatedRequiresDepartment",
                table: "ApprovalSteps");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDesignatedReviewers_RequestType_RequestId_ReviewerId",
                table: "RequestDesignatedReviewers",
                columns: new[] { "RequestType", "RequestId", "ReviewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestDesignatedReviewers_RequestType_RequestId_StepOrder",
                table: "RequestDesignatedReviewers",
                columns: new[] { "RequestType", "RequestId", "StepOrder" });
        }
    }
}
