using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCartColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddBed",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "CornerRoom",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "Preference",
                table: "Carts");

            migrationBuilder.AddColumn<int>(
                name: "RoomSettingId",
                table: "Carts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Carts_RoomSettingId",
                table: "Carts",
                column: "RoomSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_RoomSettings_RoomSettingId",
                table: "Carts",
                column: "RoomSettingId",
                principalTable: "RoomSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Carts_RoomSettings_RoomSettingId",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_Carts_RoomSettingId",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "RoomSettingId",
                table: "Carts");

            migrationBuilder.AddColumn<bool>(
                name: "AddBed",
                table: "Carts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CornerRoom",
                table: "Carts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Preference",
                table: "Carts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
