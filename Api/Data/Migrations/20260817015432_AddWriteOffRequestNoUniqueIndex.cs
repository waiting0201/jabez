using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <summary>
    /// 沖銷單號唯一索引（WriteOffRecords / TravelWriteOffRecords）。
    ///
    /// WriteOffRecords 的索引早在 20260320072900 就以手寫 SQL 建立，但 EF 設定漏宣告 HasIndex，
    /// 導致 model snapshot 一直沒有它（設定與資料庫漂移）。本次補上宣告，故建立動作一律採
    /// 「IF NOT EXISTS」寫法，既有資料庫不會因索引已存在而失敗，全新資料庫也建得起來。
    ///
    /// TravelWriteOffRecords 從未有此索引，建立前需先清洗舊資料：
    /// 空白單號（RequestNo 預設值 ""）補流水號、併發取號造成的重複單號加 -D{Id} 後綴。
    /// </summary>
    public partial class AddWriteOffRequestNoUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE WriteOffRecords
                SET RequestNo = 'WO-LEGACY-' + RIGHT('000' + CAST(Id AS NVARCHAR(10)), 3)
                WHERE RequestNo = '' OR RequestNo IS NULL;

                WITH dup AS (
                    SELECT Id, ROW_NUMBER() OVER (PARTITION BY RequestNo ORDER BY Id) AS rn
                    FROM WriteOffRecords
                )
                UPDATE w
                SET RequestNo = LEFT(w.RequestNo, 20) + '-D' + CAST(w.Id AS NVARCHAR(9))
                FROM WriteOffRecords w
                JOIN dup ON dup.Id = w.Id
                WHERE dup.rn > 1;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_WriteOffRecords_RequestNo'
                                 AND object_id = OBJECT_ID('WriteOffRecords'))
                    CREATE UNIQUE INDEX IX_WriteOffRecords_RequestNo
                    ON WriteOffRecords (RequestNo);
                """);

            migrationBuilder.Sql("""
                UPDATE TravelWriteOffRecords
                SET RequestNo = 'TWO-LEGACY-' + RIGHT('000' + CAST(Id AS NVARCHAR(10)), 3)
                WHERE RequestNo = '' OR RequestNo IS NULL;

                WITH dup AS (
                    SELECT Id, ROW_NUMBER() OVER (PARTITION BY RequestNo ORDER BY Id) AS rn
                    FROM TravelWriteOffRecords
                )
                UPDATE w
                SET RequestNo = LEFT(w.RequestNo, 20) + '-D' + CAST(w.Id AS NVARCHAR(9))
                FROM TravelWriteOffRecords w
                JOIN dup ON dup.Id = w.Id
                WHERE dup.rn > 1;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_TravelWriteOffRecords_RequestNo'
                                 AND object_id = OBJECT_ID('TravelWriteOffRecords'))
                    CREATE UNIQUE INDEX IX_TravelWriteOffRecords_RequestNo
                    ON TravelWriteOffRecords (RequestNo);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                """);
        }
    }
}
