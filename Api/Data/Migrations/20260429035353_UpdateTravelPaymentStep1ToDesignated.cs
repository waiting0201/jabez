using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTravelPaymentStep1ToDesignated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 50,
                columns: new[] { "JobTitleId", "Note", "UseApplicantDepartment", "UseApplicantDesignated" },
                values: new object[] { null, "指定初核", false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 50,
                columns: new[] { "JobTitleId", "Note", "UseApplicantDepartment", "UseApplicantDesignated" },
                values: new object[] { 4, "部門主管初核", true, false });
        }
    }
}
