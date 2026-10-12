using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * This service class is used to handle all the room category related services.
 */
public interface IRoomCategoryService
{
    // Single Room Category
    Task<RoomCategory?> GetRoomCategoryAsync(int id);
}

public class RoomCategoryService : IRoomCategoryService
{
    private readonly DB _db;

    public RoomCategoryService(DB db)
    {
        _db = db;
    }

    public async Task<RoomCategory?> GetRoomCategoryAsync(int id)
    {
        RoomCategory? roomCategory = await _db.RoomCategories
            .Include(rc => rc.Rooms)
            .ThenInclude(room => room.RoomSetting)
            .FirstOrDefaultAsync(rc => rc.IsDeleted == 0 && rc.Id == id);
        return roomCategory;
    }
}