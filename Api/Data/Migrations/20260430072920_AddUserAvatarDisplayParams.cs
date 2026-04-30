using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAvatarDisplayParams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AvatarPositionX",
                table: "Users",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 50m);

            migrationBuilder.AddColumn<decimal>(
                name: "AvatarPositionY",
                table: "Users",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 50m);

            migrationBuilder.AddColumn<decimal>(
                name: "AvatarScale",
                table: "Users",
                type: "decimal(3,2)",
                precision: 3,
                scale: 2,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "AvatarPositionX", "AvatarPositionY", "AvatarScale" },
                values: new object[] { 50m, 50m, 1m });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "AvatarPositionX", "AvatarPositionY", "AvatarScale" },
                values: new object[] { 50m, 50m, 1m });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "AvatarPositionX", "AvatarPositionY", "AvatarScale" },
                values: new object[] { 50m, 50m, 1m });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("281c2016-801e-48eb-b73b-751643464f48"),
                columns: new[] { "AvatarPositionX", "AvatarPositionY", "AvatarScale" },
                values: new object[] { 50m, 50m, 1m });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "AvatarPositionX", "AvatarPositionY", "AvatarScale" },
                values: new object[] { 50m, 50m, 1m });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("6452ad1e-9648-4194-8fb0-0ac55a76f992"),
                columns: new[] { "AvatarPositionX", "AvatarPositionY", "AvatarScale" },
                values: new object[] { 50m, 50m, 1m });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("6a4002be-23e0-4343-8092-f221b97c5098"),
                columns: new[] { "AvatarPositionX", "AvatarPositionY", "AvatarScale" },
                values: new object[] { 50m, 50m, 1m });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("83f6b1f7-2f25-4f9b-b102-37d1a27f0b35"),
                columns: new[] { "AvatarPositionX", "AvatarPositionY", "AvatarScale" },
                values: new object[] { 50m, 50m, 1m });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b56b8afd-1663-4317-9007-4560da27239d"),
                columns: new[] { "AvatarPositionX", "AvatarPositionY", "AvatarScale" },
                values: new object[] { 50m, 50m, 1m });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("df5d56ad-dd46-4fca-948c-d8301610997a"),
                columns: new[] { "AvatarPositionX", "AvatarPositionY", "AvatarScale" },
                values: new object[] { 50m, 50m, 1m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarPositionX",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AvatarPositionY",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AvatarScale",
                table: "Users");
        }
    }
}
