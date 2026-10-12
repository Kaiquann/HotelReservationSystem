using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * This service class is used to handle all the booking related operations.
 */
public interface IBookingService
{
    // Single booking
    Task<Booking?> GetBookingAsync(string id);
    Task<Booking?> GetBookingWithUserIdAsync(string id, string userId);

    // Multiple bookings
    Task<List<Booking>> GetBookingsAsync();
    Task<List<Booking>> GetBookingsWithOrderIdAsync(int orderId);
    Task<List<Booking>> GetBookingsWithRoomCategoryAsync(RoomCategory roomCategory);

    Task<List<Booking>> GetBookingsWithCheckInOutDateAsync(DateTime checkInDate,
        DateTime checkOutDate);

    Task<List<Booking>> GetBookingsWithRoomCategoryAndCheckInOutDateAsync(RoomCategory roomCategory,
        DateTime checkInDate,
        DateTime checkOutDate);

    // Custom Methods
    Task<bool> SyncExpiredBookingAsync();
    Task<bool> SetBookingCheckInAsync(string[] ids);
    Task<bool> SetBookingCheckOutAsync(string[] ids);
}

public class BookingService : IBookingService
{
    private readonly DB _db;

    public BookingService(DB db)
    {
        _db = db;
    }

    public async Task<Booking?> GetBookingAsync(string id)
    {
        Booking? booking = await _db.Bookings
            .Include(b => b.Room)
            .Include(b => b.Room.RoomCategory)
            .Include(b => b.Room.RoomSetting)
            .Include(b => b.Room.RoomCategory.RoomImages)
            .Include(b => b.User)
            .Include(b => b.Order)
            .Where(b => b.Id.ToString() == id)
            .FirstOrDefaultAsync();
        return booking;
    }

    public async Task<Booking?> GetBookingWithUserIdAsync(string id, string userId)
    {
        Booking? booking = await _db.Bookings
            .Include(b => b.Room)
            .Include(b => b.Room.RoomCategory)
            .Include(b => b.Room.RoomSetting)
            .Include(b => b.Room.RoomCategory.RoomImages)
            .Include(b => b.User)
            .Include(b => b.Order)
            .Where(b => b.Id.ToString() == id)
            .Where(b => b.UserId.ToString() == userId)
            .FirstOrDefaultAsync();
        return booking;
    }

    public async Task<List<Booking>> GetBookingsAsync()
    {
        List<Booking> bookings = await _db.Bookings
            .Include(b => b.Room)
            .Include(b => b.Room.RoomCategory)
            .Include(b => b.Room.RoomSetting)
            .Include(b => b.Room.RoomCategory.RoomImages)
            .Include(b => b.User)
            .Include(b => b.Order)
            .ToListAsync();
        return bookings;
    }

    public async Task<List<Booking>> GetBookingsWithOrderIdAsync(int orderId)
    {
        List<Booking> bookings = await _db.Bookings
            .Include(b => b.Room)
            .Include(b => b.Room.RoomCategory)
            .Include(b => b.Room.RoomSetting)
            .Include(b => b.Room.RoomCategory.RoomImages)
            .Include(b => b.User)
            .Include(b => b.Order)
            .Where(b => b.OrderId == orderId)
            .ToListAsync();
        return bookings;
    }

    public async Task<List<Booking>> GetBookingsWithRoomCategoryAsync(RoomCategory roomCategory)
    {
        List<Booking> bookings = await _db.Bookings
            .Include(b => b.Room)
            .Include(b => b.Room.RoomCategory)
            .Include(b => b.Room.RoomSetting)
            .Include(b => b.Room.RoomCategory.RoomImages)
            .Include(b => b.User)
            .Include(b => b.Order)
            .Where(b => b.Room.RoomCategoryId == roomCategory.Id)
            .ToListAsync();
        return bookings;
    }

    public async Task<List<Booking>> GetBookingsWithCheckInOutDateAsync(DateTime checkInDate,
        DateTime checkOutDate)
    {
        List<Booking> bookings = await _db.Bookings
            .Include(b => b.Room)
            .Include(b => b.Room.RoomCategory)
            .Include(b => b.Room.RoomSetting)
            .Include(b => b.Room.RoomCategory.RoomImages)
            .Include(b => b.User)
            .Include(b => b.Order)
            .Where(b => b.CheckInDate < checkOutDate &&
                        b.CheckOutDate > checkInDate)
            .ToListAsync();
        return bookings;
    }

    public async Task<List<Booking>> GetBookingsWithRoomCategoryAndCheckInOutDateAsync(RoomCategory roomCategory,
        DateTime checkInDate,
        DateTime checkOutDate)
    {
        List<Booking> bookings = await _db.Bookings
            .Include(b => b.Room)
            .Include(b => b.Room.RoomCategory)
            .Include(b => b.Room.RoomSetting)
            .Include(b => b.Room.RoomCategory.RoomImages)
            .Include(b => b.User)
            .Include(b => b.Order)
            .Where(b => b.Room.RoomCategoryId == roomCategory.Id &&
                        b.CheckInDate < checkOutDate &&
                        b.CheckOutDate > checkInDate)
            .ToListAsync();
        return bookings;
    }

    public async Task<bool> SyncExpiredBookingAsync()
    {
        DateTime today = DateTime.Today.AdjustCheckOutTime();
        await _db.Bookings
            .Where(b => today > b.CheckOutDate)
            .Where(b => b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
            .ExecuteUpdateAsync(b => b
                .SetProperty(booking => booking.Status, BookingStatus.Canceled)
            );
        return true;
    }

    public async Task<bool> SetBookingCheckInAsync(string[] ids)
    {
        try
        {
            if (ids.Length == 0) throw new Exception(Common.InvalidRequestErrorMessage);

            List<Booking> bookings = await _db.Bookings
                .Where(b => ids.Contains(b.Id.ToString()))
                .ToListAsync();

            if (bookings.Any(b => b.Status != BookingStatus.Confirmed))
                throw new Exception("Only confirmed booking(s) can be checked in");

            if (bookings.Any(b => DateTime.Today.Date < b.CheckInDate.Date))
                throw new Exception("Check-in must same as the check in date or later");

            if (bookings.Any(b => DateTime.Today.Date > b.CheckOutDate.Date))
                throw new Exception("Not allowed to check in after check-out date");

            bookings.ForEach(b =>
            {
                b.Status = BookingStatus.CheckedIn;
                b.WhenCheckInDateTime = DateTime.Now;
            });
            await _db.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task<bool> SetBookingCheckOutAsync(string[] ids)
    {
        try
        {
            if (ids.Length == 0) throw new Exception(Common.InvalidRequestErrorMessage);

            List<Booking> bookings = await _db.Bookings
                .Where(b => ids.Contains(b.Id.ToString()))
                .ToListAsync();

            if (bookings.Any(b => b.Status != BookingStatus.CheckedIn))
                throw new Exception("Only checked in booking(s) can be checked out");

            bookings.ForEach(b =>
            {
                b.Status = BookingStatus.CheckedOut;
                b.WhenCheckOutDateTime = DateTime.Now;
            });
            await _db.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}