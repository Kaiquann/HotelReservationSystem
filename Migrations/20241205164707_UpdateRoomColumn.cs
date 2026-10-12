using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoomColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoomSettingId",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomSettingId",
                table: "Rooms",
                column: "RoomSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_RoomSettings_RoomSettingId",
                table: "Rooms",
                column: "RoomSettingId",
                principalTable: "RoomSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_RoomSettings_RoomSettingId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_RoomSettingId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "RoomSettingId",
                table: "Rooms");
        }
    }
}
