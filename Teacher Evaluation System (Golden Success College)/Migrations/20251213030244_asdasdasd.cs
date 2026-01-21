using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Teacher_Evaluation_System__Golden_Success_College_.Migrations
{
    /// <inheritdoc />
    public partial class asdasdasd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationToken",
                table: "Student",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "Student",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTemporaryPassword",
                table: "Student",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpirationDate",
                table: "Student",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserId",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$2HOtBJW1u26geCa3aJVdrekM1NJPYnuPVNEXKkuWUtiQS3UVaIoWK");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserId",
                keyValue: 2,
                column: "Password",
                value: "$2a$11$JQGmSwgrg5zRdZr8eYPNBe/VOEWNJPmGWIMMJI.LMIuNR9lqLzvZa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmationToken",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "IsTemporaryPassword",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "TokenExpirationDate",
                table: "Student");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserId",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$FwcLEb/pzaBV2LjCUiHj1OhwA.7oyewydxgU/8d4wQuEbgkLqR08e");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserId",
                keyValue: 2,
                column: "Password",
                value: "$2a$11$RNMuYlYhEZ0JkooDW9dfZuMrb/lBiCQbzU3Avf2wj7YB0R0ALn8TS");
        }
    }
}
