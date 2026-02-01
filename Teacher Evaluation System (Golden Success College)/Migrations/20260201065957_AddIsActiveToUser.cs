using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Teacher_Evaluation_System__Golden_Success_College_.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "IsActive", "Password" },
                values: new object[] { true, "$2a$11$Xs3J5GrcJwLOjdLSQeYieOzPS7qu8Dbx.eW2gHS.CVGudNafFT5o6" });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "IsActive", "Password" },
                values: new object[] { true, "$2a$11$JPMNuTkwjPcDm1yi0/bTKu5jJLzNdm7b0gt0MshyiARTClUimvY4y" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "User");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserId",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$dlgk3tqgdi3gjZ9stFUVv.eRhxuEw.Z2n/BfXjEdtYmisuF.gYtNe");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserId",
                keyValue: 2,
                column: "Password",
                value: "$2a$11$Jps75RqdeioiWCCTcRwFG.KiNzlDiCa/kUA6GKX8yH2WwlCqKQmQO");
        }
    }
}
