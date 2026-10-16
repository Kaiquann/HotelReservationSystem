using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Controllers;

public class RoomController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly DB _db;
    private readonly IRoomCategoryService _roomCategoryService;
    private readonly IRoomService _roomService;

    public RoomController(DB db, IRoomService roomService, IBookingService bookingService,
        IRoomCategoryService roomCategoryService)
    {
        _db = db;
        _roomService = roomService;
        _bookingService = bookingService;
        _roomCategoryService = roomCategoryService;
    }

    public async Task<IActionResult> Index(string? search, string? filter)
    {
        ViewBag.RoomList = new SelectList(_db.RoomCategories, "Id", "Type");
        List<RoomCategory> roomCategory = await _db.GetRoomCategoryAsync();
        SelectList roomCategorySelectList = new(roomCategory, "Type", "Type", filter);
        ViewBag.RoomCategory = roomCategorySelectList;
        ViewBag.Search = search = search?.Trim() ?? "";
        ViewBag.Filter = filter = filter?.Trim() ?? "";

        List<RoomCategory> searched = await _db.RoomCategories
            .Include(r => r.RoomImages)
            .Where(r => r.Type.Contains(search) ||
                        r.Price.ToString().Contains(search))
            .Where(r => (filter == "" || r.Type == filter) &&
                        r.IsDeleted == 0)
            .ToListAsync();

        RoomVm roomVm = new RoomVm
        {
            RoomCategories = searched
        };

        if (Request.IsAjax()) return PartialView("_Index", roomVm);

        return View(roomVm);
    }

    public async Task<IActionResult> ViewRoom(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = "Invalid Id";
            return RedirectToAction("Index");
        }

        RoomCategory? roomCategory = await _db.RoomCategories
            .Include(rc => rc.RoomImages)
            .Include(rc => rc.RoomCategoryReview)
            .ThenInclude(rc => rc.User)
            .FirstOrDefaultAsync(rc => rc.IsDeleted == 0 && rc.Id == id);

        if (roomCategory == null)
        {
            TempData[SweetAlert.Danger] = "No Data found";
            return RedirectToAction("Index");
        }

        bool isCornerRoom = _db.Rooms.Any(r => r.RoomCategoryId == roomCategory.Id && r.RoomSetting.IsCornerRoom);
        bool isAllowToAddReview = false;
        bool isPendingReview = false;
        List<RoomCategoryReview> approvedReviews = roomCategory.RoomCategoryReview
            .Where(rcr => rcr.Status == RoomCategoryReviewStatus.Approved && rcr.RoomCategoryId == id)
            .ToList();

        int totalReviews = approvedReviews.Count;
        double averageRating = totalReviews > 0
            ? approvedReviews.Average(rcr => rcr.Rating)
            : 0;

        double percentage = totalReviews > 0
            ? averageRating / 5.0 * 100
            : 0;

        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser != null)
        {
            bool hasUserBooked = await _db.Bookings
                .AnyAsync(b => b.UserId == currentUser.Id && b.Room.RoomCategoryId == id);

            bool hasUserReviewed = await _db.RoomCategoryReviews
                .AnyAsync(rcr =>
                    rcr.UserId == currentUser.Id && rcr.RoomCategoryId == id &&
                    rcr.Status == RoomCategoryReviewStatus.Approved);

            isAllowToAddReview = hasUserBooked && !hasUserReviewed;
            isPendingReview = await _db.RoomCategoryReviews
                .AnyAsync(rcr =>
                    rcr.UserId == currentUser.Id && rcr.RoomCategoryId == id &&
                    rcr.Status == RoomCategoryReviewStatus.PendingReview);
        }

        ViewRoomVm viewRoomVm = new ViewRoomVm
        {
            RoomCategory = roomCategory,
            IsCornerRoom = isCornerRoom,
            isAllowToAddReview = isAllowToAddReview,
            isPendingReview = isPendingReview,
            RoomCategoryReview = approvedReviews,
            TotalReviews = totalReviews,
            AverageRating = Math.Round(averageRating, 1), // Round to 1 decimal place
            Percentage = Math.Round(percentage, 1)
        };

        return View(viewRoomVm);
    }

    [HttpGet("room-image/{id:int}")]
    public async Task<IActionResult> Image(int id)
    {
        RoomImage? image = await _db.RoomImages
            .AsNoTracking()
            .FirstOrDefaultAsync(roomImage => roomImage.Id == id);

        if (image == null) return NotFound();

        if (image.ImageData is { Length: > 0 })
            return File(image.ImageData, image.ContentType ?? "image/jpeg");

        return Redirect("/" + Common.ImageUploadPath + Path.GetFileName(image.ImagePath));
    }


    [HttpPost]
    public async Task<IActionResult> SearchRoom(RoomVm roomVm, string? search, string? filter)
    {
        ViewBag.RoomList = new SelectList(_db.RoomCategories, "Id", "Type");
        List<RoomCategory> roomCategoryList = await _db.GetRoomCategoryAsync();
        SelectList roomCategorySelectList = new(roomCategoryList, "Type", "Type", filter);
        ViewBag.RoomCategory = roomCategorySelectList;
        ViewBag.Search = search = search?.Trim() ?? "";
        ViewBag.Filter = filter = filter?.Trim() ?? "";

        if (!roomVm.CheckInDate.HasValue || !roomVm.CheckOutDate.HasValue)
        {
            TempData[SweetAlert.Warning] = "You need to select your date";
            return Request.RedirectToReferer();
        }

        if (!CheckValidDate(roomVm))
        {
            TempData[SweetAlert.Warning] = "Please choose at least one day and valid date";
            return Request.RedirectToReferer();
        }

        List<Booking> bookings =
            await _bookingService
                .GetBookingsWithCheckInOutDateAsync
                (roomVm.CheckInDate.GetValueOrDefault().AdjustCheckInTime()
                    , roomVm.CheckOutDate.GetValueOrDefault().AdjustCheckInTime());

        List<int> bookedRoomIds = bookings
            .Where(b => b.Status == BookingStatus.Confirmed ||
                        b.Status == BookingStatus.CheckedIn)
            .Select(b => b.RoomId)
            .Distinct()
            .ToList();

        List<Room> availableRooms = await _db.Rooms
            .Include(room => room.RoomSetting)
            .Where(r => !bookedRoomIds.Contains(r.Id) && r.IsDeleted == 0)
            .ToListAsync();

        List<RoomCategory> searchfilterRoomCategories = await _db.RoomCategories
            .Include(rc => rc.Rooms)
            .Include(rc => rc.RoomImages)
            .Where(rc => rc.MaxPeople >= roomVm.Guests && rc.IsDeleted == 0)
            .Where(rc => rc.Type.Contains(search) || rc.Price.ToString().Contains(search))
            .Where(rc => string.IsNullOrEmpty(filter) || rc.Type == filter)
            .Where(rc => rc.Rooms.Any(r => availableRooms.Select(ar => ar.Id).Contains(r.Id)))
            .ToListAsync();

        RoomVm filterRoomVm = new RoomVm
        {
            RoomCategories = searchfilterRoomCategories
        };

        if (Request.IsAjax()) return PartialView("_Index", filterRoomVm);

        return View("Index", filterRoomVm);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.UserOnly)]
    public async Task<IActionResult> AddRoomCategoryReview(string id, int rating, string reviewContent)
    {
        if (rating == 0 || reviewContent == null)
        {
            TempData[SweetAlert.Danger] = "Rating and Content must filled in";
            return RedirectToAction("ViewRoom", new { id });
        }

        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        RoomCategoryReview roomCategoryReview = new RoomCategoryReview
        {
            UserId = currentUser.Id,
            Rating = rating,
            Message = reviewContent,
            RoomCategoryId = int.Parse(id)
        };

        await _db.RoomCategoryReviews.AddAsync(roomCategoryReview);
        await _db.SaveChangesAsync();

        TempData[SweetAlert.Success] = "Submit successfully, need wait admin to approve";
        return RedirectToAction("ViewRoom", new { id });
    }

    private static bool CheckValidDate(RoomVm roomVm)
    {
        return roomVm.CheckInDate >= DateTime.Today && roomVm.CheckInDate < roomVm.CheckOutDate;
    }

    private static bool CheckValidDate(ViewRoomVm viewRoomVm)
    {
        return viewRoomVm.CheckInDate >= DateTime.Today && viewRoomVm.CheckInDate < viewRoomVm.CheckOutDate;
    }


    public int? GetRoomSettingId(bool isCornerRoom, string preference)
    {
        int? roomSettingId;
        switch (preference)
        {
            case Preference.SmokingArea:
                roomSettingId = _db.RoomSettings
                    .FirstOrDefault(rs => rs.IsSmokingArea && rs.IsCornerRoom == isCornerRoom)?.Id;
                break;

            case Preference.LandscapeView:
                roomSettingId = _db.RoomSettings
                    .FirstOrDefault(rs => rs.IsLandscapeArea && rs.IsCornerRoom == isCornerRoom)?.Id;
                break;

            default:
                if (isCornerRoom)
                    roomSettingId = _db.RoomSettings
                        .FirstOrDefault(rs => rs.IsCornerRoom)?.Id;
                else
                    roomSettingId = _db.RoomSettings
                        .FirstOrDefault(rs => !rs.IsCornerRoom)?.Id;
                break;
        }

        return roomSettingId;
    }

    [Authorize(Policy = AuthorizationPolicies.UserOnly)]
    [HttpPost]
    public async Task<IActionResult> ViewRoom(ViewRoomVm viewRoomVm, bool IsAddBed, bool IsCornerRoom,
        string preference)
    {
        User? user = await User.GetCurrentUser(_db);
        if (user == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        if (viewRoomVm.Preference == null)
        {
            TempData[SweetAlert.Warning] = "you need to select your preference";
            return RedirectToAction("ViewRoom");
        }

        if (viewRoomVm.CheckInDate == null || viewRoomVm.CheckOutDate == null)
        {
            TempData[SweetAlert.Warning] = "You need to select your date";
            return RedirectToAction("ViewRoom");
        }

        if (!CheckValidDate(viewRoomVm))
        {
            TempData[SweetAlert.Warning] = "Please choose at least one day and valid date";
            return RedirectToAction("ViewRoom");
        }

        DateTime checkInDateTime = viewRoomVm.CheckInDate.GetValueOrDefault().AdjustCheckInTime();
        DateTime checkOutDateTime = viewRoomVm.CheckOutDate.GetValueOrDefault().AdjustCheckOutTime();


        RoomItem roomItem = new()
        {
            RoomCategoryId = viewRoomVm.RoomCategory.Id,
            Preference = preference,
            IsAddBed = IsAddBed,
            IsCornerRoom = IsCornerRoom,
            Quantity = 1,
            CheckInDate = checkInDateTime,
            CheckOutDate = checkOutDateTime
        };

        RoomCategory? roomCategory = await _roomCategoryService.GetRoomCategoryAsync(roomItem.RoomCategoryId);
        if (roomCategory == null)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRoomCategoryErrorMessage;
            return RedirectToAction("ViewRoom");
        }

        //check room available with original room or not
        List<Room> rooms = await _roomService.GetRoomsWithPreferenceAsync(roomCategory, roomItem);
        if (rooms.Count == 0)
        {
            TempData[SweetAlert.Danger] = "Sorry,This room is not available, Please choose other room setting";
            return RedirectToAction("ViewRoom");
        }

        bool isRoomAvailable = await _roomService.IsRoomAvailableAsync(roomItem);
        if (!isRoomAvailable)
        {
            TempData[SweetAlert.Danger] = "This date is book by others, please choose other date";
            return RedirectToAction("ViewRoom");
        }

        Cart? cartList = await _db.Carts
            .Where(c =>
                c.RoomCategoryId == viewRoomVm.RoomCategory.Id &&
                c.CheckInDate == checkInDateTime &&
                c.CheckOutDate == checkOutDateTime &&
                c.IsAddBed == IsAddBed &&
                c.RoomSettingId == GetRoomSettingId(IsCornerRoom, preference) &&
                c.UserId == user.Id)
            .FirstOrDefaultAsync();

        if (cartList != null)
        {
            if (rooms.Count <= cartList.Quantity)
            {
                TempData[SweetAlert.Danger] = "Sorry,We are not enough room";
                return RedirectToAction("ViewRoom");
            }

            List<Room> availableRooms = await _roomService.GetRoomsAvailableAsync(roomItem);

            var matchingAvailableRooms = availableRooms
                .Where(r => r.RoomCategoryId == cartList.RoomCategoryId)
                .ToList();


            if (cartList.Quantity >= matchingAvailableRooms.Count)
            {
                TempData[SweetAlert.Danger] =
                    "Sorry, no more rooms are available for this category, it inside your cart already.";
                return RedirectToAction("ViewRoom");
            }

            cartList.Quantity += 1;
            cartList.TotalPrice = CalculateTotalPrice(roomCategory, viewRoomVm, IsCornerRoom, IsAddBed) *
                                  cartList.Quantity;
        }
        else
        {
            int roomSettingId = GetRoomSettingId(IsCornerRoom, preference) ?? 0;
            Cart cart = new()
            {
                UserId = user.Id,
                RoomCategoryId = roomCategory.Id,
                Quantity = 1,
                TotalPrice = CalculateTotalPrice(roomCategory, viewRoomVm, IsCornerRoom, IsAddBed),
                CheckInDate = viewRoomVm.CheckInDate.GetValueOrDefault().AdjustCheckInTime(),
                CheckOutDate = viewRoomVm.CheckOutDate.GetValueOrDefault().AdjustCheckOutTime(),
                IsAddBed = IsAddBed,
                RoomSettingId = roomSettingId
            };
            _db.Carts.Add(cart);
        }

        await _db.SaveChangesAsync();
        TempData[SweetAlert.Success] = "Added successfully.";
        return RedirectToAction("ViewRoom");
    }

    private decimal CalculateTotalPrice(RoomCategory roomCategory, ViewRoomVm? viewRoomVm, bool isCornerRoom,
        bool IsAddBed)
    {
        int totalNight = (viewRoomVm.CheckOutDate - viewRoomVm.CheckInDate).Value.Days;

        decimal totalPrice = roomCategory.Price * totalNight +
                             (isCornerRoom ? Common.CornerRoomPrice * totalNight : 0) +
                             (IsAddBed ? Common.AddBedPrice * totalNight : 0);

        return totalPrice;
    }
}
