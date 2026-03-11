using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixSeedPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "PasswordHash",
                value: "$2a$11$hBaZunc8xtFIsRVh738SJuHisvnVAsIODyfkzLxjMN.is7jZn3K7e");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "PasswordHash",
                value: "$2a$11$hBaZunc8xtFIsRVh738SJuHisvnVAsIODyfkzLxjMN.is7jZn3K7e");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "PasswordHash",
                value: "$2a$11$hBaZunc8xtFIsRVh738SJuHisvnVAsIODyfkzLxjMN.is7jZn3K7e");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "PasswordHash",
                value: "$2a$11$hBaZunc8xtFIsRVh738SJuHisvnVAsIODyfkzLxjMN.is7jZn3K7e");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "PasswordHash",
                value: "$2a$11$rBnRYkdyRVRmk5V.p8PoNuXHxUZIpXe.G/j.Q/rlOH0jMNJxqm5Fy");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "PasswordHash",
                value: "$2a$11$rBnRYkdyRVRmk5V.p8PoNuXHxUZIpXe.G/j.Q/rlOH0jMNJxqm5Fy");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "PasswordHash",
                value: "$2a$11$rBnRYkdyRVRmk5V.p8PoNuXHxUZIpXe.G/j.Q/rlOH0jMNJxqm5Fy");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "PasswordHash",
                value: "$2a$11$rBnRYkdyRVRmk5V.p8PoNuXHxUZIpXe.G/j.Q/rlOH0jMNJxqm5Fy");
        }
    }
}
