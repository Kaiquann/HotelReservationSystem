using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDBFileName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoomImage_RoomCategories_RoomCategoryId",
                table: "RoomImage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoomImage",
                table: "RoomImage");

            migrationBuilder.RenameTable(
                name: "RoomImage",
                newName: "RoomImages");

            migrationBuilder.RenameIndex(
                name: "IX_RoomImage_RoomCategoryId",
                table: "RoomImages",
                newName: "IX_RoomImages_RoomCategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoomImages",
                table: "RoomImages",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RoomImages_RoomCategories_RoomCategoryId",
                table: "RoomImages",
                column: "RoomCategoryId",
                principalTable: "RoomCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoomImages_RoomCategories_RoomCategoryId",
                table: "RoomImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoomImages",
                table: "RoomImages");

            migrationBuilder.RenameTable(
                name: "RoomImages",
                newName: "RoomImage");

            migrationBuilder.RenameIndex(
                name: "IX_RoomImages_RoomCategoryId",
                table: "RoomImage",
                newName: "IX_RoomImage_RoomCategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoomImage",
                table: "RoomImage",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RoomImage_RoomCategories_RoomCategoryId",
                table: "RoomImage",
                column: "RoomCategoryId",
                principalTable: "RoomCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
