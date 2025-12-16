using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projekt_zespołowy.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePictureAndPrefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptsEating",
                table: "PassengerProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptsPets",
                table: "PassengerProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrefersMusic",
                table: "PassengerProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "ProfilePicture",
                table: "PassengerProfiles",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptsEating",
                table: "PassengerProfiles");

            migrationBuilder.DropColumn(
                name: "AcceptsPets",
                table: "PassengerProfiles");

            migrationBuilder.DropColumn(
                name: "PrefersMusic",
                table: "PassengerProfiles");

            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "PassengerProfiles");
        }
    }
}
