using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Areas.Admin.Controllers;

[Authorize(Policy = AuthorizationPolicies.RequireAdminOrHigher)]
[Area("Admin")]
public class RoomController : Controller
{
    private readonly DB _db;

    public RoomController(DB db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, string? sort, string? dir, string? filter, int page = 1)
    {
        List<RoomCategory> roomCategory = await _db.GetRoomCategoryAsync();
        SelectList roomCategorySelectList = new(roomCategory, "Type", "Type", sort);
        ViewBag.RoomCategory = roomCategorySelectList;
        ViewBag.Search = search = search?.Trim() ?? "";
        ViewBag.Filter = filter = filter?.Trim() ?? "";

        List<Room> searched = await _db.Rooms
            .Include(r => r.RoomCategory)
            .Include(r => r.RoomSetting)
            .Where(r => (r.RoomCode.Contains(search) ||
                         r.Id.ToString().Contains(search) ||
                         r.RoomCategory.Type.Contains(search)) &&
                        (filter == "" || r.RoomCategory.Type == filter) &&
                        r.IsDeleted == 0)
            .ToListAsync();

        if (page < 1) return RedirectToAction(null, new { search, sort, dir, filter, page = 1 });

        IPagedList<Room> model = SortAndPaginateRooms(searched, sort, dir, page);

        if (page > model.PageCount && model.PageCount > 0)
            return RedirectToAction(null, new { search, sort, dir, filter, page = model.PageCount });

        if (Request.IsAjax()) return PartialView("_Index", model);

        return View(model);
    }

    public async Task<IActionResult> Deleted(string? search, string? sort, string? dir, string? filter, int page = 1)
    {
        List<RoomCategory> roomCategory = await _db.GetRoomCategoryAsync();
        SelectList roomCategorySelectList = new(roomCategory, "Type", "Type", sort);
        ViewBag.RoomCategory = roomCategorySelectList;
        ViewBag.Search = search = search?.Trim() ?? "";
        ViewBag.Filter = filter = filter?.Trim() ?? "";

        List<Room> searched = await _db.Rooms
            .Include(r => r.RoomCategory)
            .Include(r => r.RoomSetting)
            .Where(r => (r.RoomCode.Contains(search) ||
                         r.Id.ToString().Contains(search) ||
                         r.RoomCategory.Type.Contains(search)) &&
                        (filter == "" || r.RoomCategory.Type == filter) &&
                        r.IsDeleted == 1)
            .ToListAsync();

        if (page < 1) return RedirectToAction(null, new { search, sort, dir, filter, page = 1 });

        IPagedList<Room> model = SortAndPaginateRooms(searched, sort, dir, page);

        if (page > model.PageCount && model.PageCount > 0)
            return RedirectToAction(null, new { search, sort, dir, filter, page = model.PageCount });

        if (Request.IsAjax()) return PartialView("_Index", model);
        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        List<RoomCategory> roomCategory = await _db.GetRoomCategoryAsync();
        List<RoomSetting> roomSetting = await _db.RoomSettings.ToListAsync();

        CreateRoomVm createRoomVm = new()
        {
            RoomCategoryList = roomCategory,
            RoomSettingList = roomSetting
        };

        return View(createRoomVm);
    }

    [HttpPost]
    public async Task<IActionResult> Store(CreateRoomVm createRoomVm)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        try
        {
            Room newRoom = new()
            {
                RoomCategoryId = createRoomVm.RoomCategory,
                IsDeleted = 0,
                RoomCode = await GetNewCode(createRoomVm),
                RoomSettingId = createRoomVm.RoomSetting,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _db.Rooms.AddAsync(newRoom);
            await _db.SaveChangesAsync();

            TempData[SweetAlert.Success] = "Room Created Successfully";
            return RedirectToAction("Index");
        }
        catch
        {
            TempData[SweetAlert.Danger] = "An error occurred. Please try again.";
            return RedirectToAction("Index");
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        Room? room = await _db.Rooms
            .Include(r => r.RoomCategory)
            .Include(r => r.RoomSetting)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
        {
            TempData[SweetAlert.Danger] = "Room not found";
            return RedirectToAction("Index");
        }

        EditRoomVm editRoomVm = new()
        {
            Id = room.Id,
            RoomCategoryId = room.RoomCategoryId,
            RoomSettingId = room.RoomSettingId,
            RoomCategoryList = await _db.RoomCategories.Where(rc => rc.IsDeleted == 0).ToListAsync(),
            RoomSettingList = await _db.RoomSettings.ToListAsync()
        };

        return View(editRoomVm);
    }

    [HttpPost]
    public async Task<IActionResult> Update(EditRoomVm editRoomVm, int roomCategoryId, int roomSettingId)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        Room? room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == editRoomVm.Id);
        if (room == null)
        {
            TempData[SweetAlert.Danger] = "Room not found";
            return RedirectToAction("Index");
        }

        room.RoomCategoryId = roomCategoryId;
        CreateRoomVm roomVm = new()
        {
            RoomCategory = room.RoomCategoryId,
            RoomSetting = room.RoomSettingId
        };
        room.RoomCode = GetNewCode(roomVm).Result;
        room.RoomSettingId = roomSettingId;
        await _db.SaveChangesAsync();

        TempData[SweetAlert.Success] = "Room updated successfully";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string[] ids)
    {
        if (!ModelState.IsValid || ids.Length == 0)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        await _db.Rooms
            .Where(r => ids.Contains(r.Id.ToString()))
            .ForEachAsync(r => { r.IsDeleted = 1; });
        await _db.SaveChangesAsync();

        TempData[SweetAlert.Success] = $"Total {ids.Length} Room Deleted Successfully";
        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> PermanentDelete(string[] ids)
    {
        if (!ModelState.IsValid || ids.Length == 0)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        await _db.Rooms
            .Where(r => ids.Contains(r.Id.ToString()))
            .ForEachAsync(r =>
            {
                r.RoomCode = "PD:" + r.RoomCode + "_" + DateTime.Now;
                r.IsDeleted = 2;
            });

        await _db.SaveChangesAsync();

        TempData[SweetAlert.Success] = $"Total {ids.Length} Room Permanent Deleted Successfully";
        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> Recover(string[] ids)
    {
        if (!ModelState.IsValid || ids.Length == 0)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        var roomCategoryStatus = await _db.Rooms
            .Where(r => ids.Contains(r.Id.ToString()))
            .Select(r => new { r.Id, r.RoomCategoryId })
            .ToListAsync();

        List<string> validRoomIds = roomCategoryStatus
            .Where(r => _db.RoomCategories.Any(rc => rc.Id == r.RoomCategoryId && rc.IsDeleted == 0))
            .Select(r => r.Id.ToString())
            .ToList();

        List<string> invalidRoomIds = roomCategoryStatus
            .Where(r => _db.RoomCategories.Any(rc => rc.Id == r.RoomCategoryId && rc.IsDeleted == 1))
            .Select(r => r.Id.ToString())
            .ToList();

        if (invalidRoomIds.Any())
            TempData[SweetAlert.Warning] =
                $"{invalidRoomIds.Count} rooms cannot be recovered because their categories are marked as deleted.";

        if (validRoomIds.Any())
        {
            await _db.Rooms
                .Where(r => validRoomIds.Contains(r.Id.ToString()))
                .ForEachAsync(r => { r.IsDeleted = 0; });

            await _db.SaveChangesAsync();

            TempData[SweetAlert.Success] = $"{validRoomIds.Count} rooms recovered successfully.";
        }

        return Request.RedirectToReferer();
    }

    private IPagedList<Room> SortAndPaginateRooms(List<Room> searched, string? sort, string? dir, int page = 1)
    {
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        Func<Room, object> fn = sort switch
        {
            "Id" => s => s.Id,
            "RoomCategory" => s => s.RoomCategory.Type,
            "RoomCode" => s => s.RoomCode,
            "RoomSetting" => s => s.RoomSettingId,
            "CreatedAt" => s => s.CreatedAt,
            _ => s => s.Id
        };

        IOrderedEnumerable<Room> sorted = dir == "des"
            ? searched.AsEnumerable().OrderByDescending(fn)
            : searched.AsEnumerable().OrderBy(fn);

        // (3) Paging ---------------------------
        IPagedList<Room> roomPagedList = sorted.ToPagedList(page, 10);

        return roomPagedList;
    }

    private async Task<string> GetNewCode(CreateRoomVm createRoomVm)
    {
        RoomCategory? roomCategory = await _db.RoomCategories
            .FirstOrDefaultAsync(r => r.Id == createRoomVm.RoomCategory);

        if (roomCategory == null) throw new Exception("Room category not found.");

        char roomTypeChar = roomCategory.Type.FirstOrDefault();

        Room? latestRoom = await _db.Rooms
            .Where(r => r.RoomCategoryId == createRoomVm.RoomCategory && r.RoomCode.StartsWith(roomTypeChar.ToString()))
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();

        int newCodeNumber = latestRoom != null ? int.Parse(latestRoom.RoomCode.Substring(1)) + 1 : 1;

        string newRoomCode = $"{roomTypeChar}{newCodeNumber:D3}";

        return newRoomCode;
    }
}