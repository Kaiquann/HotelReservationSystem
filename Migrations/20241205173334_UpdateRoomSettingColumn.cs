using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoomSettingColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAddBed",
                table: "RoomSettings");

            migrationBuilder.AddColumn<bool>(
                name: "IsAddBed",
                table: "RoomCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAddBed",
                table: "RoomCategories");

            migrationBuilder.AddColumn<bool>(
                name: "IsAddBed",
                table: "RoomSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
