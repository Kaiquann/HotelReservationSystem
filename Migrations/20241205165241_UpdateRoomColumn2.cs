using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoomColumn2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoomSettings_RoomCategories_RoomCategoryId",
                table: "RoomSettings");

            migrationBuilder.DropIndex(
                name: "IX_RoomSettings_RoomCategoryId",
                table: "RoomSettings");

            migrationBuilder.DropColumn(
                name: "RoomCategoryId",
                table: "RoomSettings");

            migrationBuilder.DropColumn(
                name: "RoomSetting",
                table: "RoomCategories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoomCategoryId",
                table: "RoomSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoomSetting",
                table: "RoomCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RoomSettings_RoomCategoryId",
                table: "RoomSettings",
                column: "RoomCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_RoomSettings_RoomCategories_RoomCategoryId",
                table: "RoomSettings",
                column: "RoomCategoryId",
                principalTable: "RoomCategories",
                principalColumn: "Id");
        }
    }
}
