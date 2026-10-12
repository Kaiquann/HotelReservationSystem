namespace HotelReservationSystem.Controllers;

[Authorize(Policy = AuthorizationPolicies.UserOnly)]
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly DB _db;

    public BookingController(DB db, IBookingService bookingService)
    {
        _db = db;
        _bookingService = bookingService;
    }

    public async Task<IActionResult> Index(string? search, string? sort, string? dir, string? filter, int page = 1)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Login");
        }

        ViewBag.Status = EnumHelper.ToSelectList<BookingStatus>(sort);
        ViewBag.Search = search = search?.Trim() ?? "";
        ViewBag.Filter = filter = filter?.Trim() ?? "";

        List<Booking> bookings = await _bookingService.GetBookingsAsync();

        List<Booking> searched = bookings
            .Where(b =>
                b.Id.ToString().Contains(search) ||
                b.Order.OrderNumber.Contains(search) ||
                b.Room.RoomCode.Contains(search) ||
                b.Room.RoomCategory.Type.Contains(search) ||
                b.Room.RoomSetting.GetRoomPreference().Contains(search) ||
                b.CheckInDate.ToString().Contains(search) ||
                b.CheckOutDate.ToString().Contains(search))
            .Where(b => string.IsNullOrEmpty(filter) || b.Status.ToString() == filter)
            .Where(b => b.UserId == currentUser.Id)
            .ToList();

        if (page < 1) return RedirectToAction(null, new { search, sort, dir, filter, page = 1 });

        IPagedList<Booking> model = SortAndPaginateBookings(searched, sort, dir, page);

        if (page > model.PageCount && model.PageCount > 0)
            return RedirectToAction(null, new { search, sort, dir, filter, page = model.PageCount });

        if (Request.IsAjax()) return PartialView("_Index", model);

        return View(model);
    }

    public async Task<IActionResult> Show(string id)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Login");
        }

        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        Booking? booking = await _bookingService.GetBookingWithUserIdAsync(id, currentUser.Id.ToString());
        if (booking == null)
        {
            TempData[SweetAlert.Danger] = "Booking not found!";
            return RedirectToAction("Index");
        }

        BookingQrCodeVm bookingQrCodeVm = new()
        {
            BookingId = booking.Id,
            IsQrCode = true
        };
        string data = Encrypt(bookingQrCodeVm);

        string qrCodeUrl = booking.Status switch
        {
            BookingStatus.Confirmed => Url.Action("ScanQrCheckIn", "Booking", new { area = "Admin" }, Request.Scheme) +
                                       $"?data={data}",
            BookingStatus.CheckedIn => Url.Action("ScanQrCheckOut", "Booking", new { area = "Admin" }, Request.Scheme) +
                                       $"?data={data}",
            _ => string.Empty
        };

        string qrCode = QrCodeHelper.GenerateQrCode(qrCodeUrl);

        ViewBag.QrCode = qrCode;

        return View(booking);
    }

    /**
     * @description: Sort and Pagination
     */
    private IPagedList<Booking> SortAndPaginateBookings(List<Booking> searched, string? sort, string? dir, int page = 1)
    {
        // (2) Sorting --------------------------
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        Func<Booking, object> fn = sort switch
        {
            "Id" => s => s.Id,
            "Order Number" => s => s.Order.OrderNumber,
            "Room Number" => s => s.Room.RoomCode,
            "Room Type" => s => s.Room.RoomCategory.Type,
            "Status" => s => s.Status,
            "Is Add Bed?" => s => s.IsAddBed,
            "Preference" => s => s.Room.RoomSetting.GetRoomPreference(),
            "Is Corner Room?" => s => s.Room.RoomSetting.IsCornerRoom,
            "Check In Date" => s => s.CheckInDate,
            "Check Out Date" => s => s.CheckOutDate,
            "When Check In" => s => s.WhenCheckInDateTime.GetValueOrDefault(),
            "When Check Out" => s => s.WhenCheckOutDateTime.GetValueOrDefault(),
            _ => s => s.Id
        };

        IOrderedEnumerable<Booking> sorted = dir == "des"
            ? searched.AsEnumerable().OrderByDescending(fn)
            : searched.AsEnumerable().OrderBy(fn);

        // (3) Paging ---------------------------
        IPagedList<Booking> pagedList = sorted.ToPagedList(page, 10);

        return pagedList;
    }
}