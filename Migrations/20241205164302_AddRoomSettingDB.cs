using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomSettingDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAddBed",
                table: "RoomCategories");

            migrationBuilder.DropColumn(
                name: "IsCornerRoom",
                table: "RoomCategories");

            migrationBuilder.DropColumn(
                name: "IsLandscapeArea",
                table: "RoomCategories");

            migrationBuilder.DropColumn(
                name: "IsSmokingArea",
                table: "RoomCategories");

            migrationBuilder.AddColumn<int>(
                name: "RoomSetting",
                table: "RoomCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RoomSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsSmokingArea = table.Column<bool>(type: "bit", nullable: false),
                    IsLandscapeArea = table.Column<bool>(type: "bit", nullable: false),
                    IsCornerRoom = table.Column<bool>(type: "bit", nullable: false),
                    IsAddBed = table.Column<bool>(type: "bit", nullable: false),
                    RoomCategoryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomSettings_RoomCategories_RoomCategoryId",
                        column: x => x.RoomCategoryId,
                        principalTable: "RoomCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomSettings_RoomCategoryId",
                table: "RoomSettings",
                column: "RoomCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomSettings");

            migrationBuilder.DropColumn(
                name: "RoomSetting",
                table: "RoomCategories");

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

            migrationBuilder.AddColumn<bool>(
                name: "IsSmokingArea",
                table: "RoomCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
