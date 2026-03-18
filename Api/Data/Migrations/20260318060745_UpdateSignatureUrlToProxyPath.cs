using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSignatureUrlToProxyPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 將舊的 Azure Blob URL 轉為 API 代理路徑
            // 舊格式: https://xxx.blob.core.windows.net/signatures/userId.png
            // 新格式: files/signatures/userId.png
            migrationBuilder.Sql("""
                UPDATE Users
                SET SignatureUrl = 'files/signatures/' +
                    SUBSTRING(SignatureUrl,
                        CHARINDEX('/signatures/', SignatureUrl) + LEN('/signatures/'),
                        LEN(SignatureUrl))
                WHERE SignatureUrl IS NOT NULL
                  AND SignatureUrl LIKE '%/signatures/%'
                  AND SignatureUrl LIKE 'http%'
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
