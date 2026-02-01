using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Teacher_Evaluation_System__Golden_Success_College_.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Student",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserId",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$XOr54Sr0Vvq9/JLcla6xzej/dJmkkl2BLS6gxraZrEarz3PG4GtLy");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserId",
                keyValue: 2,
                column: "Password",
                value: "$2a$11$VwvK5UFm1lY36n07h/A9MO/wdbs5nNW4nbzrkb2zfX2rlez9LDNDC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Student");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserId",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$Xs3J5GrcJwLOjdLSQeYieOzPS7qu8Dbx.eW2gHS.CVGudNafFT5o6");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "UserId",
                keyValue: 2,
                column: "Password",
                value: "$2a$11$JPMNuTkwjPcDm1yi0/bTKu5jJLzNdm7b0gt0MshyiARTClUimvY4y");
        }
    }
}
