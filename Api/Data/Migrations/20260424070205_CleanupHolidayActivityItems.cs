using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CleanupHolidayActivityItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 建立「待清理」旗標表。實際的資料與 Blob 清理由 Program.cs 啟動時執行，
            // 以便發生錯誤時能 log 並繼續啟動，不至於因資料清理失敗阻擋 Functions host。
            migrationBuilder.Sql("""
                IF OBJECT_ID('dbo.__HolidayBlobCleanup', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.__HolidayBlobCleanup (
                        Id       INT IDENTITY(1,1) PRIMARY KEY,
                        FileUrl  NVARCHAR(2000) NULL
                    );
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID('dbo.__HolidayBlobCleanup', 'U') IS NOT NULL DROP TABLE dbo.__HolidayBlobCleanup;");
        }
    }
}
