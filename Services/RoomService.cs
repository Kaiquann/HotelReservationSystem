using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * This service class is used to handle all the room related services.
 */
public interface IRoomService
{
    // Single Room
    Task<Room?> GetRoomAsync(string id);

    // Multiple Rooms
    Task<List<Room>> GetRoomsWithPreferenceAsync(RoomCategory roomCategory, RoomItem roomItem);

    // Custom Methods
    Task<List<Room>> GetRoomsAvailableAsync(RoomItem roomItem);
    Task<bool> IsRoomAvailableAsync(RoomItem roomItem);
    Task<bool> IsRoomAvailableAsync(List<RoomItem> roomItems);
}

public class RoomService : IRoomService
{
    private readonly IBookingService _bookingService;
    private readonly DB _db;

    public RoomService(DB db, IBookingService bookingService)
    {
        _db = db;
        _bookingService = bookingService;
    }

    public async Task<Room?> GetRoomAsync(string id)
    {
        return await _db.Rooms
            .Include(r => r.RoomCategory)
            .Include(r => r.RoomSetting)
            .Where(r => r.Id.ToString() == id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Room>> GetRoomsWithPreferenceAsync(RoomCategory roomCategory, RoomItem roomItem)
    {
        List<Room> rooms = await _db.Rooms
            .Where(r => r.RoomCategoryId == roomCategory.Id && r.IsDeleted == 0)
            .Where(r => (roomItem.Preference == Preference.LandscapeView && r.RoomSetting.IsLandscapeArea) ||
                        (roomItem.Preference == Preference.SmokingArea && r.RoomSetting.IsSmokingArea))
            .Where(r => r.RoomSetting.IsCornerRoom == roomItem.IsCornerRoom)
            .ToListAsync();
        return rooms;
    }

    public async Task<List<Room>> GetRoomsAvailableAsync(RoomItem roomItem)
    {
        // Get all bookings with the check-in and check-out date
        List<Booking> bookings =
            await _bookingService.GetBookingsWithCheckInOutDateAsync(roomItem.CheckInDate, roomItem.CheckOutDate);

        // Get all available rooms
        List<Room> availableRooms = await _db.Rooms
            .Include(room => room.RoomSetting)
            // Filter out rooms that are not in the same room category
            .Where(r => r.RoomCategoryId == roomItem.RoomCategoryId && r.IsDeleted == 0)
            // Filter out rooms that do not match the preference
            .Where(r => (roomItem.Preference == Preference.LandscapeView && r.RoomSetting.IsLandscapeArea) ||
                        (roomItem.Preference == Preference.SmokingArea && r.RoomSetting.IsSmokingArea))
            // Filter out rooms that are not corner rooms
            .Where(r => r.RoomSetting.IsCornerRoom == roomItem.IsCornerRoom)
            .ToListAsync();

        // Filter out rooms that are canceled, failed, refunded, pending, or checked out
        availableRooms = availableRooms
            .Where(r => bookings.All(b => b.RoomId != r.Id ||
                                          (b.Status != BookingStatus.Confirmed &&
                                           b.Status != BookingStatus.CheckedIn)))
            .ToList();

        return availableRooms;
    }

    public async Task<bool> IsRoomAvailableAsync(RoomItem roomItem)
    {
        List<Room> availableRooms = await GetRoomsAvailableAsync(roomItem);
        return availableRooms.Count() >= roomItem.Quantity;
    }


    public async Task<bool> IsRoomAvailableAsync(List<RoomItem> roomItems)
    {
        bool isValid = false;
        foreach (RoomItem roomItem in roomItems)
        {
            isValid = await IsRoomAvailableAsync(roomItem);
            if (!isValid) break;
        }

        return isValid;
    }
}