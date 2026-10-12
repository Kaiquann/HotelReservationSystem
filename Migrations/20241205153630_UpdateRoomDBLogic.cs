using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoomDBLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CornerRoom",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "LandscapeArea",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "SmokingArea",
                table: "Rooms");

            migrationBuilder.RenameColumn(
                name: "AddBed",
                table: "RoomCategories",
                newName: "IsSmokingArea");

            migrationBuilder.AddColumn<bool>(
                name: "IsAddBed",
                table: "RoomCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCornerRoom",
                table: "RoomCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLandscapeArea",
                table: "RoomCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomCategoryId",
                table: "Rooms",
                column: "RoomCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_RoomCategories_RoomCategoryId",
                table: "Rooms",
                column: "RoomCategoryId",
                principalTable: "RoomCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_RoomCategories_RoomCategoryId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_RoomCategoryId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "IsAddBed",
                table: "RoomCategories");

            migrationBuilder.DropColumn(
                name: "IsCornerRoom",
                table: "RoomCategories");

            migrationBuilder.DropColumn(
                name: "IsLandscapeArea",
                table: "RoomCategories");

            migrationBuilder.RenameColumn(
                name: "IsSmokingArea",
                table: "RoomCategories",
                newName: "AddBed");

            migrationBuilder.AddColumn<bool>(
                name: "CornerRoom",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LandscapeArea",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmokingArea",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
