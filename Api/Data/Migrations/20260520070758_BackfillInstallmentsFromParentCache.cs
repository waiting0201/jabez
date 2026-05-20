using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <summary>
    /// Phase 2 過渡 — 把 4 個父表（PaymentRequest / AdvanceRequest / TravelRequest / TravelPaymentRequest）
    /// 已有 EstimatedPaymentDate 但無對應 installments 的舊資料，轉成單一期 installment。
    /// 必須在 RemovePaymentDateCacheFromParents（DROP COLUMN）之前部署，避免歷史資料遺失。
    /// </summary>
    public partial class BackfillInstallmentsFromParentCache : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO PaymentRequestInstallments
                  (PaymentRequestId, InstallmentNo, Amount, ExpectedDate, PaidAt, PaidByUserId, Note, CreatedAt, UpdatedAt)
                SELECT p.Id, 1, p.TotalAmount, p.EstimatedPaymentDate, p.PaidAt, p.PaidByUserId, NULL, GETUTCDATE(), GETUTCDATE()
                FROM PaymentRequests p
                WHERE p.EstimatedPaymentDate IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM PaymentRequestInstallments i WHERE i.PaymentRequestId = p.Id);
                """);

            migrationBuilder.Sql("""
                INSERT INTO AdvanceRequestInstallments
                  (AdvanceRequestId, InstallmentNo, Amount, ExpectedDate, PaidAt, PaidByUserId, Note, CreatedAt, UpdatedAt)
                SELECT a.Id, 1, a.GrandTotal, a.EstimatedPaymentDate, a.PaidAt, a.PaidByUserId, NULL, GETUTCDATE(), GETUTCDATE()
                FROM AdvanceRequests a
                WHERE a.EstimatedPaymentDate IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM AdvanceRequestInstallments i WHERE i.AdvanceRequestId = a.Id);
                """);

            migrationBuilder.Sql("""
                INSERT INTO TravelRequestInstallments
                  (TravelRequestId, InstallmentNo, Amount, ExpectedDate, PaidAt, PaidByUserId, Note, CreatedAt, UpdatedAt)
                SELECT t.Id, 1, t.GrandTotal, t.EstimatedPaymentDate, t.PaidAt, t.PaidByUserId, NULL, GETUTCDATE(), GETUTCDATE()
                FROM TravelRequests t
                WHERE t.EstimatedPaymentDate IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM TravelRequestInstallments i WHERE i.TravelRequestId = t.Id);
                """);

            migrationBuilder.Sql("""
                INSERT INTO TravelPaymentRequestInstallments
                  (TravelPaymentRequestId, InstallmentNo, Amount, ExpectedDate, PaidAt, PaidByUserId, Note, CreatedAt, UpdatedAt)
                SELECT tp.Id, 1, tp.GrandTotal, tp.EstimatedPaymentDate, tp.PaidAt, tp.PaidByUserId, NULL, GETUTCDATE(), GETUTCDATE()
                FROM TravelPaymentRequests tp
                WHERE tp.EstimatedPaymentDate IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM TravelPaymentRequestInstallments i WHERE i.TravelPaymentRequestId = tp.Id);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 無法可靠 reverse：backfill 插入的列無法區分原本就有的 installments。
            // 若需要 rollback，請從備份還原父表 cache 欄位後再 drop 整批 backfill 列（手動）。
        }
    }
}
