using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projekt_zespołowy.Migrations
{
    /// <inheritdoc />
    public partial class freeeezeee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FrozenAmount",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Balance",
                table: "AspNetUsers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FrozenAmount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Balance",
                table: "AspNetUsers");
        }
    }
}
