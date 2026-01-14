using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projekt_zespołowy.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalDataToApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "DriverApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdDocumentNumber",
                table: "DriverApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "DriverApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SecondName",
                table: "DriverApplications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "DriverApplications");

            migrationBuilder.DropColumn(
                name: "IdDocumentNumber",
                table: "DriverApplications");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "DriverApplications");

            migrationBuilder.DropColumn(
                name: "SecondName",
                table: "DriverApplications");
        }
    }
}
