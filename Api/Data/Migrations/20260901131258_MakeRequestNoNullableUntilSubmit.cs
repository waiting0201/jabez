using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <summary>
    /// 申請單號改為「送簽時取號」（2026-09）：RequestNo 由 NOT NULL 改為可為 NULL，草稿階段不配號。
    ///
    /// 原本 7 種申請單在 CreateAsync 建立草稿當下就取號，造成單號日期是建單日而非送簽日，
    /// 且草稿被刪除會在當日流水號留下缺號。改由 SubmitAsync 呼叫 Common/RequestNoGenerator 取號。
    ///
    /// 唯一索引一律改為 filtered（WHERE RequestNo IS NOT NULL）—— SQL Server 的一般唯一索引
    /// 視多個 NULL 為互相衝突，不加 filter 會在第二張草稿就撞索引。
    /// 索引動作採 IF EXISTS / IF NOT EXISTS 寫法，既有資料庫與全新資料庫都能套用。
    ///
    /// 既有資料一律不動：已取號的單（含目前仍是草稿者）送簽時因 RequestNo 非空而不會重新配號，
    /// 故上線前的單號日期基準仍是建單日，之後才是送簽日。
    /// </summary>
    public partial class MakeRequestNoNullableUntilSubmit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_WriteOffRecords_RequestNo'
                             AND object_id = OBJECT_ID('WriteOffRecords'))
                    DROP INDEX IX_WriteOffRecords_RequestNo ON WriteOffRecords;

                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_TravelWriteOffRecords_RequestNo'
                             AND object_id = OBJECT_ID('TravelWriteOffRecords'))
                    DROP INDEX IX_TravelWriteOffRecords_RequestNo ON TravelWriteOffRecords;

                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_TravelRequests_RequestNo'
                             AND object_id = OBJECT_ID('TravelRequests'))
                    DROP INDEX IX_TravelRequests_RequestNo ON TravelRequests;

                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_TravelPaymentRequests_RequestNo'
                             AND object_id = OBJECT_ID('TravelPaymentRequests'))
                    DROP INDEX IX_TravelPaymentRequests_RequestNo ON TravelPaymentRequests;

                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_PreReviewRequests_RequestNo'
                             AND object_id = OBJECT_ID('PreReviewRequests'))
                    DROP INDEX IX_PreReviewRequests_RequestNo ON PreReviewRequests;

                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_PaymentRequests_RequestNo'
                             AND object_id = OBJECT_ID('PaymentRequests'))
                    DROP INDEX IX_PaymentRequests_RequestNo ON PaymentRequests;

                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_AdvanceRequests_RequestNo'
                             AND object_id = OBJECT_ID('AdvanceRequests'))
                    DROP INDEX IX_AdvanceRequests_RequestNo ON AdvanceRequests;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "WriteOffRecords",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "TravelWriteOffRecords",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "TravelRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "TravelPaymentRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "PreReviewRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "PaymentRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "AdvanceRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            // 兩張沖銷表原有 HasDefaultValue("")，歷史上可能殘留空字串單號。
            // '' 不是有效單號，且 filtered index（WHERE RequestNo IS NOT NULL）擋不住多筆 ''，
            // 留著會讓第二張草稿撞唯一索引，故一併正規化為 NULL（不影響任何有效單號）。
            migrationBuilder.Sql("""
                UPDATE WriteOffRecords       SET RequestNo = NULL WHERE RequestNo = '';
                UPDATE TravelWriteOffRecords SET RequestNo = NULL WHERE RequestNo = '';
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_WriteOffRecords_RequestNo'
                                 AND object_id = OBJECT_ID('WriteOffRecords'))
                    CREATE UNIQUE INDEX IX_WriteOffRecords_RequestNo
                    ON WriteOffRecords (RequestNo) WHERE RequestNo IS NOT NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_TravelWriteOffRecords_RequestNo'
                                 AND object_id = OBJECT_ID('TravelWriteOffRecords'))
                    CREATE UNIQUE INDEX IX_TravelWriteOffRecords_RequestNo
                    ON TravelWriteOffRecords (RequestNo) WHERE RequestNo IS NOT NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_TravelRequests_RequestNo'
                                 AND object_id = OBJECT_ID('TravelRequests'))
                    CREATE UNIQUE INDEX IX_TravelRequests_RequestNo
                    ON TravelRequests (RequestNo) WHERE RequestNo IS NOT NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_TravelPaymentRequests_RequestNo'
                                 AND object_id = OBJECT_ID('TravelPaymentRequests'))
                    CREATE UNIQUE INDEX IX_TravelPaymentRequests_RequestNo
                    ON TravelPaymentRequests (RequestNo) WHERE RequestNo IS NOT NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_PreReviewRequests_RequestNo'
                                 AND object_id = OBJECT_ID('PreReviewRequests'))
                    CREATE UNIQUE INDEX IX_PreReviewRequests_RequestNo
                    ON PreReviewRequests (RequestNo) WHERE RequestNo IS NOT NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_PaymentRequests_RequestNo'
                                 AND object_id = OBJECT_ID('PaymentRequests'))
                    CREATE UNIQUE INDEX IX_PaymentRequests_RequestNo
                    ON PaymentRequests (RequestNo) WHERE RequestNo IS NOT NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_AdvanceRequests_RequestNo'
                                 AND object_id = OBJECT_ID('AdvanceRequests'))
                    CREATE UNIQUE INDEX IX_AdvanceRequests_RequestNo
                    ON AdvanceRequests (RequestNo) WHERE RequestNo IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滾前先補值：欄位改回 NOT NULL 時現有 NULL 會失敗，且回滾後的唯一索引不再是 filtered，
            // 故以 Id 組出唯一的暫時單號（僅回滾情境使用，正常流程不會出現 DRAFT- 開頭的單號）。
            migrationBuilder.Sql("""
                UPDATE PaymentRequests       SET RequestNo = 'DRAFT-PR-'  + CAST(Id AS NVARCHAR(20)) WHERE RequestNo IS NULL;
                UPDATE PreReviewRequests     SET RequestNo = 'DRAFT-PRV-' + CAST(Id AS NVARCHAR(20)) WHERE RequestNo IS NULL;
                UPDATE AdvanceRequests       SET RequestNo = 'DRAFT-ADV-' + CAST(Id AS NVARCHAR(20)) WHERE RequestNo IS NULL;
                UPDATE TravelRequests        SET RequestNo = 'DRAFT-TR-'  + CAST(Id AS NVARCHAR(20)) WHERE RequestNo IS NULL;
                UPDATE TravelPaymentRequests SET RequestNo = 'DRAFT-TPR-' + CAST(Id AS NVARCHAR(20)) WHERE RequestNo IS NULL;
                UPDATE WriteOffRecords       SET RequestNo = 'DRAFT-WO-'  + CAST(Id AS NVARCHAR(20)) WHERE RequestNo IS NULL;
                UPDATE TravelWriteOffRecords SET RequestNo = 'DRAFT-TWO-' + CAST(Id AS NVARCHAR(20)) WHERE RequestNo IS NULL;
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_WriteOffRecords_RequestNo'
                             AND object_id = OBJECT_ID('WriteOffRecords'))
                    DROP INDEX IX_WriteOffRecords_RequestNo ON WriteOffRecords;

                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_TravelWriteOffRecords_RequestNo'
                             AND object_id = OBJECT_ID('TravelWriteOffRecords'))
                    DROP INDEX IX_TravelWriteOffRecords_RequestNo ON TravelWriteOffRecords;

                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_TravelRequests_RequestNo'
                             AND object_id = OBJECT_ID('TravelRequests'))
                    DROP INDEX IX_TravelRequests_RequestNo ON TravelRequests;

                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_TravelPaymentRequests_RequestNo'
                             AND object_id = OBJECT_ID('TravelPaymentRequests'))
                    DROP INDEX IX_TravelPaymentRequests_RequestNo ON TravelPaymentRequests;

                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_PreReviewRequests_RequestNo'
                             AND object_id = OBJECT_ID('PreReviewRequests'))
                    DROP INDEX IX_PreReviewRequests_RequestNo ON PreReviewRequests;

                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_PaymentRequests_RequestNo'
                             AND object_id = OBJECT_ID('PaymentRequests'))
                    DROP INDEX IX_PaymentRequests_RequestNo ON PaymentRequests;

                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_AdvanceRequests_RequestNo'
                             AND object_id = OBJECT_ID('AdvanceRequests'))
                    DROP INDEX IX_AdvanceRequests_RequestNo ON AdvanceRequests;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "WriteOffRecords",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "TravelWriteOffRecords",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "TravelRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "TravelPaymentRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "PreReviewRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "PaymentRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequestNo",
                table: "AdvanceRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WriteOffRecords_RequestNo",
                table: "WriteOffRecords",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelWriteOffRecords_RequestNo",
                table: "TravelWriteOffRecords",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_RequestNo",
                table: "TravelRequests",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelPaymentRequests_RequestNo",
                table: "TravelPaymentRequests",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreReviewRequests_RequestNo",
                table: "PreReviewRequests",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_RequestNo",
                table: "PaymentRequests",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequests_RequestNo",
                table: "AdvanceRequests",
                column: "RequestNo",
                unique: true);
        }
    }
}
