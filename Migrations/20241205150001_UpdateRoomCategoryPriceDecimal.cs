using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoomCategoryPriceDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CornerRoom",
                table: "Carts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Carts_RoomCategoryId",
                table: "Carts",
                column: "RoomCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_RoomCategories_RoomCategoryId",
                table: "Carts",
                column: "RoomCategoryId",
                principalTable: "RoomCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Carts_RoomCategories_RoomCategoryId",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_Carts_RoomCategoryId",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "CornerRoom",
                table: "Carts");
        }
    }
}
