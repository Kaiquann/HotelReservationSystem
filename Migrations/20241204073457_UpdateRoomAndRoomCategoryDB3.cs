using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoomAndRoomCategoryDB3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Floor",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "RoomCode",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "Block",
                table: "RoomCategories");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "RoomCategories");

            migrationBuilder.AddColumn<bool>(
                name: "CornerRoom",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_RoomImage_RoomCategoryId",
                table: "RoomImage",
                column: "RoomCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_RoomImage_RoomCategories_RoomCategoryId",
                table: "RoomImage",
                column: "RoomCategoryId",
                principalTable: "RoomCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoomImage_RoomCategories_RoomCategoryId",
                table: "RoomImage");

            migrationBuilder.DropIndex(
                name: "IX_RoomImage_RoomCategoryId",
                table: "RoomImage");

            migrationBuilder.DropColumn(
                name: "CornerRoom",
                table: "Rooms");

            migrationBuilder.AddColumn<int>(
                name: "Floor",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RoomCode",
                table: "Rooms",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Block",
                table: "RoomCategories",
                type: "nvarchar(1)",
                maxLength: 1,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "RoomCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
