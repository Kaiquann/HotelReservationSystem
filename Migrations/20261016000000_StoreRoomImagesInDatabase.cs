using HotelReservationSystem.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations;

[DbContext(typeof(DB))]
[Migration("20261016000000_StoreRoomImagesInDatabase")]
public partial class StoreRoomImagesInDatabase : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "ImageData",
            table: "RoomImages",
            type: "varbinary(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ContentType",
            table: "RoomImages",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ImageData", table: "RoomImages");
        migrationBuilder.DropColumn(name: "ContentType", table: "RoomImages");
    }
}
