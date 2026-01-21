using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Teacher_Evaluation_System__Golden_Success_College_.Migrations
{
    /// <inheritdoc />
    public partial class evaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "User",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "Student",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "Student",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "Password", "ResetToken", "ResetTokenExpiry" },
                values: new object[] { "$2a$11$dlgk3tqgdi3gjZ9stFUVv.eRhxuEw.Z2n/BfXjEdtYmisuF.gYtNe", null, null });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "Password", "ResetToken", "ResetTokenExpiry" },
                values: new object[] { "$2a$11$Jps75RqdeioiWCCTcRwFG.KiNzlDiCa/kUA6GKX8yH2WwlCqKQmQO", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiry",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiry",
                table: "Student");

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
    }
}
