using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projekt_zespołowy.Migrations
{
    /// <inheritdoc />
    public partial class RenameSmokingToIsSmoker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PrefersNonSmoking",
                table: "PassengerProfiles",
                newName: "IsSmoker");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsSmoker",
                table: "PassengerProfiles",
                newName: "PrefersNonSmoking");
        }
    }
}
