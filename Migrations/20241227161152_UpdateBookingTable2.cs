using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookingTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CheckOutDateTime",
                table: "Bookings",
                newName: "WhenCheckOutDateTime");

            migrationBuilder.RenameColumn(
                name: "CheckInDateTime",
                table: "Bookings",
                newName: "WhenCheckInDateTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WhenCheckOutDateTime",
                table: "Bookings",
                newName: "CheckOutDateTime");

            migrationBuilder.RenameColumn(
                name: "WhenCheckInDateTime",
                table: "Bookings",
                newName: "CheckInDateTime");
        }
    }
}
