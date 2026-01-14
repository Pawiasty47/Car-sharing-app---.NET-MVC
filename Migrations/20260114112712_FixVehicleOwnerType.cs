using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projekt_zespołowy.Migrations
{
    /// <inheritdoc />
    public partial class FixVehicleOwnerType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_DriverProfiles_OwnerId",
                table: "Vehicles");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "Vehicles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DriverProfileUserId",
                table: "Vehicles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_DriverProfileUserId",
                table: "Vehicles",
                column: "DriverProfileUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_AspNetUsers_OwnerId",
                table: "Vehicles",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_DriverProfiles_DriverProfileUserId",
                table: "Vehicles",
                column: "DriverProfileUserId",
                principalTable: "DriverProfiles",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_AspNetUsers_OwnerId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_DriverProfiles_DriverProfileUserId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_DriverProfileUserId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "DriverProfileUserId",
                table: "Vehicles");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "Vehicles",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_DriverProfiles_OwnerId",
                table: "Vehicles",
                column: "OwnerId",
                principalTable: "DriverProfiles",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
