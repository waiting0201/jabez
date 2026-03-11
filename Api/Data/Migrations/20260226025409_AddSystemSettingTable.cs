using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SiteDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Timezone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SessionTimeoutMinutes = table.Column<int>(type: "int", nullable: false),
                    AllowRegistration = table.Column<bool>(type: "bit", nullable: false),
                    RequireEmailVerification = table.Column<bool>(type: "bit", nullable: false),
                    MaintenanceMode = table.Column<bool>(type: "bit", nullable: false),
                    MaintenanceMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    WorkStartTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    WorkEndTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    MonthlyOvertimeLimit = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "AllowRegistration", "ContactEmail", "Language", "MaintenanceMessage", "MaintenanceMode", "MonthlyOvertimeLimit", "RequireEmailVerification", "SessionTimeoutMinutes", "SiteDescription", "SiteName", "SiteUrl", "Timezone", "WorkEndTime", "WorkStartTime" },
                values: new object[] { 1, false, "admin@jabez.com", "zh-TW", "System is under maintenance. Please try again later.", false, 46, true, 60, "Enterprise administration portal", "Jabez Admin", "https://admin.jabez.com", "Asia/Taipei", "18:00", "09:00" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
